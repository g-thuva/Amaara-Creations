using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Identity;

public sealed class SecondFactorService(UserManager<User> users, IUserStore<User> store, ApplicationDbContext db,
    IDataProtectionProvider protection, IAuditService audit, IRequestActor actor, TimeProvider clock) : ISecondFactorService
{
    private const string Provider = "Amaara.SecondFactor.v1";
    // Fixed stripes bound memory while serializing unit/in-memory operations.
    private static readonly SemaphoreSlim[] Locks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1)).ToArray();
    private IDataProtector Protector => protection.CreateProtector(Provider);
    private sealed record Enrollment(string Key, string Stamp, long Expires);
    private sealed record UsedCode(string Hash, long Expires);

    public async Task<SecondFactorStatus> StatusAsync(User user, CancellationToken ct) =>
        new(user.TwoFactorEnabled, (await Read<List<string>>(user, "Recovery") ?? []).Count, (await users.GetRolesAsync(user)).Any(AppRoles.AdministrativeRoles.Contains));

    public Task<AuthenticatorEnrollment?> BeginAsync(User user, string password, string? code, string? recoveryCode, CancellationToken ct) => Locked(user, async () =>
    {
        if (!await Password(user, password) || (user.TwoFactorEnabled && !await VerifyCore(user, code, recoveryCode))) return null;
        var key = users.GenerateNewAuthenticatorKey();
        var expires = clock.GetUtcNow().AddMinutes(10);
        await Write(user, "Enrollment", new Enrollment(key, user.SecurityStamp!, expires.ToUnixTimeSeconds()));
        await Record("AuthenticatorEnrollmentStarted", user, ct);
        return new AuthenticatorEnrollment(key, $"otpauth://totp/{Uri.EscapeDataString("Amaara Creations:" + user.Email)}?secret={key}&issuer=Amaara%20Creations&digits=6&period=30", expires);
    }, ct);

    public Task<IReadOnlyList<string>?> ConfirmAsync(User user, string password, string code, CancellationToken ct) => Locked<IReadOnlyList<string>?>(user, async () =>
    {
        if (!await Password(user, password)) return null;
        var pending = await Read<Enrollment>(user, "Enrollment");
        if (pending == null || pending.Stamp != user.SecurityStamp || pending.Expires <= clock.GetUtcNow().ToUnixTimeSeconds() || !Matches(pending.Key, code))
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        await ((IUserAuthenticatorKeyStore<User>)store).SetAuthenticatorKeyAsync(user, pending.Key, ct);
        Ensure(await users.SetTwoFactorEnabledAsync(user, true));
        Ensure(await users.UpdateSecurityStampAsync(user));
        await users.RemoveAuthenticationTokenAsync(user, Provider, "Enrollment");
        await Write(user, "Used", new List<UsedCode> { new(CodeHash(user, Clean(code)), clock.GetUtcNow().AddMinutes(3).ToUnixTimeSeconds()) });
        var codes = await NewRecovery(user);
        Ensure(await users.ResetAccessFailedCountAsync(user));
        await Record("AuthenticatorEnabled", user, ct);
        return codes;
    }, ct);

    public Task<bool> VerifyAsync(User user, string? code, string? recoveryCode, CancellationToken ct) => Locked(user, () => VerifyCore(user, code, recoveryCode), ct);

    public Task<IReadOnlyList<string>?> RegenerateAsync(User user, string password, string code, CancellationToken ct) => Locked<IReadOnlyList<string>?>(user, async () =>
    {
        if (!await Password(user, password) || !await VerifyCore(user, code, null)) return null;
        var codes = await NewRecovery(user);
        await Record("AuthenticatorRecoveryCodesRegenerated", user, ct);
        return codes;
    }, ct);

    public Task<bool> DisableAsync(User user, string password, string? code, string? recoveryCode, CancellationToken ct) => Locked(user, async () =>
    {
        if (!await Password(user, password) || !await VerifyCore(user, code, recoveryCode)) return false;
        // A staff member can replace a lost authenticator through verified recovery,
        // but cannot turn the mandatory staff second factor off.
        if ((await users.GetRolesAsync(user)).Any(AppRoles.AdministrativeRoles.Contains)) return false;
        Ensure(await users.SetTwoFactorEnabledAsync(user, false));
        Ensure(await users.ResetAuthenticatorKeyAsync(user));
        await Write(user, "Recovery", new List<string>());
        await users.RemoveAuthenticationTokenAsync(user, Provider, "Enrollment");
        await Record("AuthenticatorDisabled", user, ct);
        return true;
    }, ct);

    private async Task<bool> Password(User user, string password)
    {
        if (await users.IsLockedOutAsync(user)) return false;
        if (await users.CheckPasswordAsync(user, password)) return true;
        await users.AccessFailedAsync(user);
        return false;
    }

    private async Task<bool> VerifyCore(User user, string? code, string? recoveryCode)
    {
        if (!user.TwoFactorEnabled || await users.IsLockedOutAsync(user)) return false;
        var valid = false;
        if (!string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(recoveryCode))
        {
            var key = await users.GetAuthenticatorKeyAsync(user);
            var cleaned = Clean(code);
            var used = (await Read<List<UsedCode>>(user, "Used") ?? []).Where(x => x.Expires > clock.GetUtcNow().ToUnixTimeSeconds()).ToList();
            var hash = CodeHash(user, cleaned);
            if (key != null && Matches(key, cleaned) && !used.Any(x => x.Hash == hash))
            {
                used.Add(new(hash, clock.GetUtcNow().AddMinutes(3).ToUnixTimeSeconds()));
                await Write(user, "Used", used);
                valid = true;
            }
        }
        else if (!string.IsNullOrWhiteSpace(recoveryCode) && string.IsNullOrWhiteSpace(code))
        {
            var codes = await Read<List<string>>(user, "Recovery") ?? [];
            var hash = Hash(Clean(recoveryCode).ToUpperInvariant());
            var index = codes.FindIndex(x => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(x), Convert.FromHexString(hash)));
            if (index >= 0)
            {
                codes.RemoveAt(index);
                await Write(user, "Recovery", codes);
                valid = true;
            }
        }
        if (!valid) await users.AccessFailedAsync(user);
        return valid;
    }

    private bool Matches(string key, string? input)
    {
        var code = Clean(input);
        if (code.Length != 6 || code.Any(x => x is < '0' or > '9')) return false;
        var secret = DecodeBase32(key);
        var step = clock.GetUtcNow().ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        // Permit one step of clock drift in each direction; consumption is shared
        // across API instances and is retained longer than this acceptance window.
        for (var offset = -1; offset <= 1; offset++)
        {
            BinaryPrimitives.WriteInt64BigEndian(counter, step + offset);
            var mac = HMACSHA1.HashData(secret, counter);
            var index = mac[^1] & 15;
            var value = (BinaryPrimitives.ReadUInt32BigEndian(mac.AsSpan(index, 4)) & 0x7fffffff) % 1_000_000;
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(value.ToString("D6")), Encoding.ASCII.GetBytes(code))) return true;
        }
        return false;
    }

    private async Task<IReadOnlyList<string>> NewRecovery(User user)
    {
        var codes = Enumerable.Range(0, 10).Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(12))).ToArray();
        await Write(user, "Recovery", codes.Select(Hash).ToList());
        return codes;
    }

    private async Task<T?> Read<T>(User user, string name)
    {
        var value = await users.GetAuthenticationTokenAsync(user, Provider, name);
        if (value == null) return default;
        try { return JsonSerializer.Deserialize<T>(Protector.Unprotect(value)); }
        catch (Exception ex) when (ex is CryptographicException or JsonException) { return default; }
    }
    private async Task Write<T>(User user, string name, T value) => Ensure(await users.SetAuthenticationTokenAsync(user, Provider, name, Protector.Protect(JsonSerializer.Serialize(value))));
    private static string Clean(string? value) => (value ?? "").Replace(" ", "").Replace("-", "");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string CodeHash(User user, string code) => Hash(user.Id + ":" + code);
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("The security settings could not be saved. Try again."); }
    private Task Record(string action, User user, CancellationToken ct) => audit.RecordAsync(action, nameof(User), user.Id, null, new { Enabled = user.TwoFactorEnabled }, actor.Audit, ct);

    private async Task<T> Locked<T>(User user, Func<Task<T>> operation, CancellationToken ct)
    {
        if (db.Database.IsSqlServer())
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var resource = "Amaara.SecondFactor." + Hash(user.Id);
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @r < 0 THROW 51002, 'Authentication lock unavailable', 1;", ct);
            await db.Entry(user).ReloadAsync(ct);
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        var stripe = Locks[(int)(BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(user.Id)), 0) % Locks.Length)];
        await stripe.WaitAsync(ct);
        try { return await operation(); }
        finally { stripe.Release(); }
    }

    private static byte[] DecodeBase32(string key)
    {
        var output = new List<byte>(); var buffer = 0; var bits = 0;
        foreach (var character in key.ToUpperInvariant().TrimEnd('='))
        {
            var value = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(character);
            if (value < 0) throw new InvalidOperationException("Invalid authenticator configuration.");
            buffer = (buffer << 5) | value; bits += 5;
            if (bits >= 8) { bits -= 8; output.Add((byte)(buffer >> bits)); }
        }
        return output.ToArray();
    }
}

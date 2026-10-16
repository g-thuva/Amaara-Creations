using System.Reflection;
using be.Api.Controllers;

namespace be.Tests.Architecture;
public sealed class ModuleBoundaryTests
{
    [Theory]
    [InlineData("be.Domain", "be.Application")]
    [InlineData("be.Domain", "be.Infrastructure")]
    [InlineData("be.Domain", "be.Api")]
    [InlineData("be.Domain", "Microsoft.EntityFrameworkCore")]
    [InlineData("be.Application", "be.Infrastructure")]
    [InlineData("be.Application", "be.Api")]
    [InlineData("be.Application", "Microsoft.AspNetCore.Mvc")]
    [InlineData("be.Application", "Microsoft.AspNetCore.Http")]
    public void ModuleInterfacesDoNotExposeForbiddenDependencies(string owner, string forbidden)
    {
        var violations = typeof(AuthController).Assembly.GetTypes().Where(type => type.Namespace?.StartsWith(owner, StringComparison.Ordinal) == true).SelectMany(type => ReferencedTypes(type).Where(reference => reference.Namespace?.StartsWith(forbidden, StringComparison.Ordinal) == true).Select(reference => $"{type.FullName} -> {reference.FullName}")).Distinct().ToArray();
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Application", "be.Api")]
    [InlineData("Application", "be.Infrastructure")]
    [InlineData("Domain", "be.Application")]
    [InlineData("Domain", "be.Infrastructure")]
    [InlineData("Domain", "be.Api")]
    [InlineData("Infrastructure", "be.Api")]
    public void ImplementationsDoNotReachIntoDisallowedLayers(string folder, string forbidden)
    {
        var violations = Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Path, "be", folder), "*.cs", SearchOption.AllDirectories).Where(path => !path.Split(System.IO.Path.DirectorySeparatorChar).Contains("Migrations")).Where(path => File.ReadAllText(path).Contains(forbidden + ".", StringComparison.Ordinal)).Select(path => System.IO.Path.GetRelativePath(RepositoryRoot.Path, path)).ToArray();
        Assert.Empty(violations);
    }

    [Fact]
    public void ControllersKeepPersistenceAndProviderImplementationsOutsideHttpActions()
    {
        var violations = Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Path, "be", "Api", "Controllers"), "*.cs").Where(path => new[] { "Microsoft.EntityFrameworkCore", "be.Infrastructure", "DbSet<", "FileStream(", "new HttpClient(" }.Any(fragment => File.ReadAllText(path).Contains(fragment, StringComparison.Ordinal))).Select(System.IO.Path.GetFileName).ToArray();
        Assert.Empty(violations);
    }

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var references = type.GetFields(flags).Select(field => field.FieldType).Concat(type.GetProperties(flags).Select(property => property.PropertyType)).Concat(type.GetConstructors(flags).SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))).Concat(type.GetMethods(flags).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))).Concat(type.GetInterfaces());
        if (type.BaseType != null)
            references = references.Append(type.BaseType);
        return references.SelectMany(Flatten);
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.HasElementType)
            foreach (var element in Flatten(type.GetElementType()!))
                yield return element;
        foreach (var argument in type.GetGenericArguments())
            foreach (var element in Flatten(argument))
                yield return element;
    }
}

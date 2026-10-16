# Preserve provider identity and currency across payment history

Orders and attempts retain their stored provider, mode and currency. Separate Zahls and historical Datatrans adapters implement neutral gateway/webhook interfaces; reconciliation and refunds route by recorded provider, while active checkout selection remains explicit. CHF and LKR are separate monetary histories with no implicit conversion or relabelling, so adapter changes cannot reinterpret settled funds or make TEST results appear LIVE.

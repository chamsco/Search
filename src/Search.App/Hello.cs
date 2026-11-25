using System.Security.Cryptography;
using Windows.Security.Credentials.UI;
using Search.Core;

namespace Search.App;

/// <summary>Secrets kept for this Windows account (DPAPI): what the vault's items are protected with.</summary>
public sealed class DpapiProtector : ISecretProtector
{
    static readonly byte[] Entropy = System.Text.Encoding.UTF8.GetBytes("Search vault");
    public byte[] Protect(byte[] plain) => ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
    public byte[] Unprotect(byte[] protectedBytes) => ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
}

/// <summary>A password is shown only to the person the PC belongs to: Windows Hello (face, fingerprint, PIN), or whatever the account takes (Vault.swift's prove). Where Windows can't ask at all, only an account with nothing set up has nothing to prove; any other reason it gives refuses, an error is never taken for a yes.</summary>
public static class Hello
{
    /// <summary>One yes holds for a minute for that site alone (its registrable domain), as Chrome's does, so a sign-in that wants its password again a moment later isn't asked twice.</summary>
    static readonly Dictionary<string, DateTime> Yes = [];
    static bool asking;
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(60);
    /// <summary>How many times a question was put to the person; a test run reads it.</summary>
    public static int Asked { get; private set; }

    public static bool Fresh(string? host)
    {
        var key = host is null ? "" : Registrable.Domain(host);
        return Yes.TryGetValue(key, out var at) && DateTime.UtcNow - at < Grace;
    }

    public static async Task<bool> Prove(string reason, string? host = null)
    {
        if (host is not null && Fresh(host)) return true;
        if (asking) return false; // a second question while one is up gets no answer
        asking = true;
        try
        {
            UserConsentVerifierAvailability available;
            try { available = await UserConsentVerifier.CheckAvailabilityAsync(); } catch { return false; }
            if (available is UserConsentVerifierAvailability.DeviceNotPresent or UserConsentVerifierAvailability.NotConfiguredForUser) return true; // nothing is set up: nothing to prove
            if (available != UserConsentVerifierAvailability.Available) return false;
            Asked++;
            var result = await UserConsentVerifier.RequestVerificationAsync(reason);
            var ok = result == UserConsentVerificationResult.Verified;
            if (ok) Yes[host is null ? "" : Registrable.Domain(host)] = DateTime.UtcNow;
            return ok;
        }
        catch { return false; }
        finally { asking = false; }
    }
}

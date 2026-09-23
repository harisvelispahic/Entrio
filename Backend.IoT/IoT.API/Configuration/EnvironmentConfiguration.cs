namespace IoT.API.Configuration;

/// <summary>
/// Bridges the repo-root <c>.env</c> file to ASP.NET Core configuration.
///
/// WHY THIS EXISTS
/// ---------------
/// One secret should have exactly one name. The <c>.env</c> file uses plain, readable
/// names (<c>JWT_KEY</c>), while ASP.NET Core binds environment variables using the
/// <c>Section__Key</c> convention (<c>Jwt__Key</c> -&gt; <c>Jwt:Key</c>). Rather than
/// writing every secret twice, <see cref="ApplyAliases"/> copies each plain name onto
/// its configuration name at startup.
///
/// THE TWO ENVIRONMENTS
/// --------------------
/// Local run : no env vars are set, so <see cref="LoadDotEnvFile"/> reads the repo-root
///             <c>.env</c> and <see cref="ApplyAliases"/> maps the plain names across.
/// Container : there is no <c>.env</c> file (loading is a silent no-op) and
///             docker-compose sets the <c>Section__Key</c> names directly in its
///             <c>environment:</c> block.
///
/// The <see cref="Aliases"/> table below and the compose <c>environment:</c> block
/// express the SAME mapping. Adding a secret means touching three places together:
/// <c>.env.example</c>, this table, and <c>docker-compose.yml</c>.
///
/// Values already present in the environment always win — <c>NoClobber</c> on the
/// DotNetEnv load, and the null check in <see cref="ApplyAliases"/> — so a value
/// supplied by compose is never overwritten by a stray <c>.env</c>.
/// </summary>
public static class EnvironmentConfiguration
{
    /// <summary>
    /// Plain <c>.env</c> name -&gt; ASP.NET Core environment-variable name.
    ///
    /// ONLY values that are IDENTICAL in both environments belong here. Host-dependent
    /// values must NOT be aliased, because one plain name cannot hold two values:
    /// the connection string points at the compose service name inside the container
    /// but at <c>localhost,1437</c> on the host. That one lives at the bottom of
    /// <c>.env</c> as an explicit <c>ConnectionStrings__DefaultConnection</c> line.
    /// </summary>
    private static readonly (string PlainName, string ConfigName)[] Aliases =
    {
        ("JWT_KEY",      "Jwt__Key"),
        ("DEVICE_KEY",   "Seed__DeviceKey"),
        ("OWNER_EMAIL",  "Seed__OwnerEmail"),
        ("OWNER_PIN",    "Seed__OwnerPin"),
    };

    /// <summary>
    /// Loads the repo-root <c>.env</c> into environment variables.
    /// Must be called BEFORE <c>WebApplication.CreateBuilder</c>, because the default
    /// configuration sources are read there and <c>AddEnvironmentVariables()</c> only
    /// sees what already exists.
    /// A missing <c>.env</c> — the normal case inside a container — is a silent no-op.
    /// </summary>
    public static void LoadDotEnvFile()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");

            if (File.Exists(candidate))
            {
                // NoClobber: never overwrite a variable the environment already set,
                // so compose-provided values always win over the file.
                DotNetEnv.Env.NoClobber().Load(candidate);
                return;
            }

            directory = directory.Parent;
        }
    }

    /// <summary>
    /// Copies each plain <c>.env</c> name onto its <c>Section__Key</c> counterpart.
    /// Skips any alias whose target is already set, so an explicit
    /// <c>Section__Key</c> line in <c>.env</c> or in compose always wins.
    /// Must also be called before <c>WebApplication.CreateBuilder</c>.
    /// </summary>
    public static void ApplyAliases()
    {
        foreach (var (plainName, configName) in Aliases)
        {
            var value = Environment.GetEnvironmentVariable(plainName);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(configName)))
                continue;

            Environment.SetEnvironmentVariable(configName, value);
        }
    }
}

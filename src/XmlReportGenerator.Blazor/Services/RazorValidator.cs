using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace XmlReportGenerator.Blazor.Services;

/// <summary>
/// Validates generated Blazor components (.razor) by attempting to compile them
/// using the Razor language services and Roslyn.
/// </summary>
public class RazorValidator
{
    private readonly ILogger<RazorValidator> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="RazorValidator"/>.
    /// </summary>
    public RazorValidator(ILogger<RazorValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates the given Razor component source code.
    /// </summary>
    /// <param name="razorContent">The content of the <c>.razor</c> file to validate.</param>
    /// <param name="componentName">An optional component name used for diagnostics (defaults to <c>GeneratedComponent</c>).</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether compilation succeeded and any diagnostics.
    /// </returns>
    public ValidationResult Validate(string razorContent, string componentName = "GeneratedComponent")
    {
        if (string.IsNullOrWhiteSpace(razorContent))
        {
            return ValidationResult.Failure("Razor content is null or empty.");
        }

        try
        {
            // ── Step 1: Parse via Razor Language Services ─────────────────────
            var engine = RazorProjectEngine.Create(
                RazorConfiguration.Default,
                RazorProjectFileSystem.Create("."),
                _ => { });

            var document = RazorSourceDocument.Create(razorContent, $"{componentName}.razor");

            var codeDocument = engine.Process(
                document,
                fileKind: null,
                importSources: Array.Empty<RazorSourceDocument>(),
                tagHelpers: Array.Empty<Microsoft.AspNetCore.Razor.Language.TagHelperDescriptor>());

            // Collect Razor-level diagnostics from the syntax tree
            var syntaxTree = codeDocument.GetSyntaxTree();
            var razorErrors = syntaxTree.Diagnostics
                .Where(d => d.Severity == RazorDiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (razorErrors.Count > 0)
            {
                _logger.LogWarning("Razor parsing produced {Count} error(s).", razorErrors.Count);
                return ValidationResult.Failure(razorErrors);
            }

            // ── Step 2: Attempt a basic C# parse on the generated code ────────
            var generatedCSharp = codeDocument.GetCSharpDocument().GeneratedCode;
            var csharpTree = CSharpSyntaxTree.ParseText(generatedCSharp);
            var csharpErrors = csharpTree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (csharpErrors.Count > 0)
            {
                _logger.LogWarning("C# parsing produced {Count} error(s).", csharpErrors.Count);
                return ValidationResult.Failure(csharpErrors);
            }

            _logger.LogInformation("Razor component '{Name}' validated successfully.", componentName);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating Razor component '{Name}'.", componentName);
            return ValidationResult.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Represents the result of a Razor component validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Gets a value indicating whether validation succeeded.</summary>
    public bool IsValid { get; private init; }

    /// <summary>Gets the list of diagnostic error messages (empty when valid).</summary>
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    private ValidationResult() { }

    /// <summary>Creates a successful <see cref="ValidationResult"/>.</summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>Creates a failed <see cref="ValidationResult"/> with the given error message.</summary>
    public static ValidationResult Failure(string error) =>
        new() { IsValid = false, Errors = new[] { error } };

    /// <summary>Creates a failed <see cref="ValidationResult"/> with multiple error messages.</summary>
    public static ValidationResult Failure(IReadOnlyList<string> errors) =>
        new() { IsValid = false, Errors = errors };
}

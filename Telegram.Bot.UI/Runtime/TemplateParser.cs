namespace Telegram.Bot.UI.Runtime;

/// <summary>
/// Parses {{ expression }} templates with proper bracket counting.
/// Unlike regex, correctly handles nested braces and strings.
/// </summary>
public static class TemplateParser {
    /// <summary>
    /// Represents a parsed template expression with its position.
    /// </summary>
    public readonly record struct TemplateMatch(int Start, int End, string Expression);

    /// <summary>
    /// Quick check if string contains any template expressions.
    /// </summary>
    public static bool ContainsTemplates(string? input) {
        if (string.IsNullOrEmpty(input)) {
            return false;
        }
        return input.Contains("{{");
    }

    /// <summary>
    /// Parse all {{ expression }} templates from input string.
    /// Correctly handles:
    /// - Nested braces: {{ { key: value } }}
    /// - Strings with braces: {{ "text with } inside" }}
    /// - Template literals: {{ `text ${var}` }}
    /// </summary>
    public static List<TemplateMatch> Parse(string input) {
        var results = new List<TemplateMatch>();

        if (string.IsNullOrEmpty(input)) {
            return results;
        }

        int i = 0;
        while (i < input.Length - 1) {
            // Look for {{
            if (input[i] == '{' && input[i + 1] == '{') {
                int start = i;
                i += 2; // Skip {{

                // Skip leading whitespace
                while (i < input.Length && char.IsWhiteSpace(input[i])) {
                    i++;
                }

                int exprStart = i;

                // Find matching }} with proper bracket counting
                int braceDepth = 0;
                int exprEnd = -1;

                while (i < input.Length - 1) {
                    char c = input[i];

                    // Handle strings - skip their content
                    if (c == '"' || c == '\'' || c == '`') {
                        i = SkipString(input, i, c);
                        continue;
                    }

                    // Count braces
                    if (c == '{') {
                        braceDepth++;
                        i++;
                        continue;
                    }

                    if (c == '}') {
                        if (braceDepth > 0) {
                            braceDepth--;
                            i++;
                            continue;
                        }

                        // Check for }}
                        if (i + 1 < input.Length && input[i + 1] == '}') {
                            // Found closing }}
                            exprEnd = i;

                            // Trim trailing whitespace from expression
                            while (exprEnd > exprStart && char.IsWhiteSpace(input[exprEnd - 1])) {
                                exprEnd--;
                            }

                            var expression = input[exprStart..exprEnd];
                            results.Add(new TemplateMatch(start, i + 2, expression));

                            i += 2; // Skip }}
                            break;
                        }
                    }

                    i++;
                }

                // If we didn't find closing }}, move past the opening {{
                if (exprEnd == -1) {
                    i = start + 2;
                }
            } else {
                i++;
            }
        }

        return results;
    }

    /// <summary>
    /// Skip a string literal, handling escape sequences.
    /// Returns position after the closing quote.
    /// </summary>
    private static int SkipString(string input, int start, char quote) {
        int i = start + 1; // Skip opening quote

        while (i < input.Length) {
            char c = input[i];

            if (c == '\\' && i + 1 < input.Length) {
                // Skip escaped character
                i += 2;
                continue;
            }

            if (c == quote) {
                return i + 1; // Position after closing quote
            }

            // Handle template literal ${} - don't count these braces
            if (quote == '`' && c == '$' && i + 1 < input.Length && input[i + 1] == '{') {
                // Skip ${...} inside template literal
                i += 2;
                int depth = 1;
                while (i < input.Length && depth > 0) {
                    if (input[i] == '{') {
                        depth++;
                    } else if (input[i] == '}') {
                        depth--;
                    } else if (input[i] == '"' || input[i] == '\'') {
                        i = SkipString(input, i, input[i]);
                        continue;
                    }
                    i++;
                }
                continue;
            }

            i++;
        }

        return i; // Unclosed string, return end of input
    }

    /// <summary>
    /// Render a template by evaluating all expressions.
    /// </summary>
    /// <param name="template">Template string with {{ expr }} placeholders</param>
    /// <param name="evaluator">Function to evaluate each expression</param>
    /// <returns>Rendered string with expressions replaced by their values</returns>
    public static string Render(string template, Func<string, string> evaluator) {
        if (string.IsNullOrEmpty(template)) {
            return template;
        }

        var matches = Parse(template);
        if (matches.Count == 0) {
            return template;
        }

        // Build result by replacing matches from end to start (preserves positions)
        var result = template;
        for (int i = matches.Count - 1; i >= 0; i--) {
            var match = matches[i];
            var value = evaluator(match.Expression);
            result = result[..match.Start] + value + result[match.End..];
        }

        return result;
    }

    /// <summary>
    /// Async version of Render for expressions that need async evaluation.
    /// </summary>
    public static async Task<string> RenderAsync(string template, Func<string, Task<string>> evaluator) {
        if (string.IsNullOrEmpty(template)) {
            return template;
        }

        var matches = Parse(template);
        if (matches.Count == 0) {
            return template;
        }

        // Evaluate all expressions (can be parallelized if needed)
        var values = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++) {
            values[i] = await evaluator(matches[i].Expression);
        }

        // Build result from end to start
        var result = template;
        for (int i = matches.Count - 1; i >= 0; i--) {
            var match = matches[i];
            result = result[..match.Start] + values[i] + result[match.End..];
        }

        return result;
    }
}

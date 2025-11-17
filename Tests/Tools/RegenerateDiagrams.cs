using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BetterTradersGuild.RoomContents;
using BetterTradersGuild.Tests.Helpers;

namespace BetterTradersGuild.Tests.Tools
{
    /// <summary>
    /// Regenerates all test diagrams by parsing test code and extracting parameters.
    /// Run this after modifying placement formulas to update all diagram comments.
    /// </summary>
    class RegenerateDiagrams
    {
        static void Main(string[] args)
        {
            string testFilePath = @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BetterTradersGuild\Tests\RoomContents\PlacementCalculatorTests.cs";

            if (args.Length > 0)
            {
                testFilePath = args[0];
            }

            Console.WriteLine($"Regenerating diagrams in: {testFilePath}");
            Console.WriteLine();

            string content = File.ReadAllText(testFilePath);
            string updated = RegenerateAllDiagrams(content);

            File.WriteAllText(testFilePath, updated);
            Console.WriteLine("Done! Diagrams regenerated successfully.");
        }

        static string RegenerateAllDiagrams(string content)
        {
            // Pattern to match CalculateBestPlacement calls
            // Matches: var result = CalculateBestPlacement(new SimpleRect { MinX = 0, MinZ = 0, Width = 12, Height = 10 }, prefabSize: 6, doors: ...)
            var placementPattern = new Regex(
                @"var result = CalculateBestPlacement\(\s*" +
                @"new SimpleRect \{ MinX = (\d+), MinZ = (\d+), Width = (\d+), Height = (\d+) \},\s*" +
                @"prefabSize:\s*(\d+),\s*" +
                @"doors:.*?\}\);",
                RegexOptions.Singleline);

            // Pattern to extract expected values from assertions
            var centerXPattern = new Regex(@"Assert\.Equal\((\d+), result\.CenterX\);");
            var centerZPattern = new Regex(@"Assert\.Equal\((\d+), result\.CenterZ\);");
            var rotationPattern = new Regex(@"Assert\.Equal\((\d+), result\.Rotation\);");

            var matches = placementPattern.Matches(content);
            Console.WriteLine($"Found {matches.Count} placement calculations");

            foreach (Match match in matches)
            {
                try
                {
                    // Extract parameters from function call
                    int minX = int.Parse(match.Groups[1].Value);
                    int minZ = int.Parse(match.Groups[2].Value);
                    int width = int.Parse(match.Groups[3].Value);
                    int height = int.Parse(match.Groups[4].Value);
                    int prefabSize = int.Parse(match.Groups[5].Value);

                    // Find the test method containing this match
                    int methodStart = FindMethodStart(content, match.Index);
                    int methodEnd = FindMethodEnd(content, match.Index);
                    string methodContent = content.Substring(methodStart, methodEnd - methodStart);

                    Console.WriteLine($"  Method span: {methodStart} to {methodEnd} (length {methodEnd - methodStart})");

                    // DEBUG: Show if assertions exist in content
                    bool hasAssertEqualCenterX = methodContent.Contains("Assert.Equal") && methodContent.Contains("CenterX");
                    Console.WriteLine($"  Contains 'Assert.Equal' and 'CenterX': {hasAssertEqualCenterX}");

                    // Extract expected values from assertions
                    var centerXMatch = centerXPattern.Match(methodContent);
                    var centerZMatch = centerZPattern.Match(methodContent);
                    var rotationMatch = rotationPattern.Match(methodContent);

                    if (!centerXMatch.Success || !centerZMatch.Success || !rotationMatch.Success)
                    {
                        Console.WriteLine($"  Skipping placement - missing assertions (X:{centerXMatch.Success}, Z:{centerZMatch.Success}, Rot:{rotationMatch.Success})");
                        continue;
                    }

                    int expectedCenterX = int.Parse(centerXMatch.Groups[1].Value);
                    int expectedCenterZ = int.Parse(centerZMatch.Groups[1].Value);
                    int expectedRotation = int.Parse(rotationMatch.Groups[1].Value);

                    // Extract door positions from the doors list
                    var doorPositions = ExtractDoorPositions(match.Value);
                    Console.WriteLine($"  Found {doorPositions.Count} door positions");

                    // Generate argument description
                    string description = DiagramGenerator.GenerateArgumentDescription(
                        roomWidth: width,
                        roomHeight: height,
                        centerX: expectedCenterX,
                        centerZ: expectedCenterZ,
                        rotation: expectedRotation,
                        prefabSize: prefabSize);

                    // Generate diagram
                    string diagram = DiagramGenerator.GenerateRoomDiagram(
                        roomWidth: width,
                        roomHeight: height,
                        prefabCenterX: expectedCenterX,
                        prefabCenterZ: expectedCenterZ,
                        rotation: expectedRotation,
                        doorPositions: doorPositions,
                        showCoordinates: true);

                    Console.WriteLine($"  Generated diagram: {width}×{height} room, center=({expectedCenterX},{expectedCenterZ}), rot={expectedRotation}");

                    // Find and replace diagram region (includes description)
                    content = ReplaceDiagramRegion(content, methodStart, methodEnd, description, diagram);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error processing match: {ex.Message}");
                }
            }

            return content;
        }

        static int FindMethodStart(string content, int matchIndex)
        {
            // Search backwards for [Fact] or [Theory] attribute
            int pos = matchIndex;
            while (pos > 100)
            {
                // Look at a reasonable window (3000 chars to accommodate large diagram comments)
                int windowStart = Math.Max(0, pos - 3000);
                int windowLength = pos - windowStart;
                string window = content.Substring(windowStart, windowLength);

                if (window.Contains("[Fact]") || window.Contains("[Theory]"))
                {
                    // Found the attribute, find the start of its line
                    int attrPos = window.LastIndexOf("[Fact");
                    if (attrPos == -1)
                        attrPos = window.LastIndexOf("[Theory");

                    // Find the start of the line containing the attribute
                    int attrLineStart = attrPos;
                    while (attrLineStart > 0 && window[attrLineStart - 1] != '\n')
                        attrLineStart--;

                    // Check if there's already a comment block before the attribute
                    int commentStart = -1;
                    for (int i = attrLineStart - 1; i >= 0; i--)
                    {
                        if (i > 0 && window[i] == '*' && window[i - 1] == '/')
                        {
                            commentStart = i - 1;
                            break;
                        }
                        // Stop if we hit non-whitespace/non-comment content
                        char c = window[i];
                        if (c != ' ' && c != '\t' && c != '\n' && c != '\r' && c != '*' && c != '/')
                            break;
                    }

                    // If comment exists before attribute, return comment start; otherwise return attribute line start
                    return windowStart + (commentStart != -1 ? commentStart : attrLineStart);
                }

                pos -= 150; // Move back in chunks
            }
            return 0;
        }

        static int FindMethodEnd(string content, int matchIndex)
        {
            // Find the method's opening brace first
            int methodBraceStart = -1;
            for (int i = matchIndex; i >= 0; i--)
            {
                if (content[i] == '{')
                {
                    // Check if this is the method brace (comes after "public void")
                    int checkStart = Math.Max(0, i - 100);
                    string preceding = content.Substring(checkStart, i - checkStart);
                    if (preceding.Contains("public void") || preceding.Contains("public static"))
                    {
                        methodBraceStart = i;
                        break;
                    }
                }
            }

            if (methodBraceStart == -1)
                return content.Length;

            // Now count braces from the method start to find its closing brace
            int braceCount = 0;
            for (int i = methodBraceStart; i < content.Length; i++)
            {
                if (content[i] == '{')
                    braceCount++;
                else if (content[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                        return i + 1;
                }
            }

            return content.Length;
        }

        static string ReplaceDiagramRegion(string content, int methodStart, int methodEnd, string description, string newDiagram)
        {
            string methodContent = content.Substring(methodStart, methodEnd - methodStart);

            // Find <!-- DIAGRAM_START --> and <!-- DIAGRAM_END --> markers
            int startMarker = methodContent.IndexOf("<!-- DIAGRAM_START -->");
            int endMarker = methodContent.IndexOf("<!-- DIAGRAM_END -->");

            if (startMarker == -1 || endMarker == -1)
            {
                Console.WriteLine("    No diagram markers found - adding them");

                // Look for [Fact] or [Theory] in method content
                int newAttrPos = methodContent.IndexOf("[Fact]");
                if (newAttrPos == -1)
                    newAttrPos = methodContent.IndexOf("[Theory]");

                if (newAttrPos != -1)
                {
                    // Find the start of the line containing the attribute
                    int attrLineStart = newAttrPos;
                    while (attrLineStart > 0 && methodContent[attrLineStart - 1] != '\n')
                        attrLineStart--;

                    // Get indentation from attribute line
                    string indent = GetIndentation(methodContent, attrLineStart);

                    // Build comment block
                    var commentBuilder = new StringBuilder();
                    commentBuilder.Append(FormatDescriptionAndDiagramWithMarkers(description, newDiagram, indent));
                    commentBuilder.AppendLine();
                    commentBuilder.Append(indent);
                    commentBuilder.AppendLine("*/");
                    commentBuilder.Append(indent);  // Preserve indentation before [Fact]

                    // Insert comment BEFORE [Fact]
                    methodContent = methodContent.Insert(attrLineStart, commentBuilder.ToString());
                    content = content.Remove(methodStart, methodEnd - methodStart);
                    content = content.Insert(methodStart, methodContent);
                }

                return content;
            }

            // Markers exist - need to replace comment
            // First, find and remove the old comment
            int oldCommentStart = methodContent.IndexOf("/*");
            if (oldCommentStart == -1)
                return content;

            int oldCommentEnd = methodContent.IndexOf("*/", endMarker);
            if (oldCommentEnd == -1)
                return content;

            // Check if comment is after [Fact] (inside method body)
            int attrPos = methodContent.IndexOf("[Fact]");
            if (attrPos == -1)
                attrPos = methodContent.IndexOf("[Theory]");

            if (attrPos != -1)
            {
                // Find the attribute line start for indentation
                int attrLineStart = attrPos;
                while (attrLineStart > 0 && methodContent[attrLineStart - 1] != '\n')
                    attrLineStart--;

                string indent = GetIndentation(methodContent, attrLineStart);

                // Remove old comment from wherever it is
                int removeStart = methodStart + oldCommentStart;
                int removeEnd = methodStart + oldCommentEnd + 2; // +2 for "*/"

                // Find if there's extra whitespace after the comment to remove
                int extraWhitespace = 0;
                while (removeEnd + extraWhitespace < content.Length &&
                       (content[removeEnd + extraWhitespace] == '\n' || content[removeEnd + extraWhitespace] == '\r'))
                {
                    extraWhitespace++;
                }

                content = content.Remove(removeStart, removeEnd - removeStart + extraWhitespace);

                // Adjust methodEnd since we removed content
                methodEnd -= (removeEnd - removeStart + extraWhitespace);

                // Recalculate method content and find [Fact] position again
                methodContent = content.Substring(methodStart, methodEnd - methodStart);
                attrPos = methodContent.IndexOf("[Fact]");
                if (attrPos == -1)
                    attrPos = methodContent.IndexOf("[Theory]");

                if (attrPos != -1)
                {
                    attrLineStart = attrPos;
                    while (attrLineStart > 0 && methodContent[attrLineStart - 1] != '\n')
                        attrLineStart--;

                    // Build new comment block and insert before [Fact]
                    var commentBuilder = new StringBuilder();
                    commentBuilder.Append(FormatDescriptionAndDiagramWithMarkers(description, newDiagram, indent));
                    commentBuilder.AppendLine();
                    commentBuilder.Append(indent);
                    commentBuilder.AppendLine("*/");
                    commentBuilder.Append(indent);

                    methodContent = methodContent.Insert(attrLineStart, commentBuilder.ToString());
                    content = content.Remove(methodStart, methodEnd - methodStart);
                    content = content.Insert(methodStart, methodContent);
                }
            }

            return content;
        }

        static string GetIndentation(string content, int pos)
        {
            // Find the start of the line
            int lineStart = pos;
            while (lineStart > 0 && content[lineStart - 1] != '\n')
                lineStart--;

            // Count leading spaces/tabs
            int i = lineStart;
            while (i < content.Length && (content[i] == ' ' || content[i] == '\t'))
                i++;

            return content.Substring(lineStart, i - lineStart);
        }

        static string FormatDescriptionAndDiagramWithMarkers(string description, string diagram, string baseIndentation)
        {
            var sb = new StringBuilder();

            // Add opening of comment block with first description line
            var descLines = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (descLines.Length > 0)
            {
                sb.Append(baseIndentation);
                sb.AppendLine("/*  " + descLines[0]);

                // Add remaining description lines
                for (int i = 1; i < descLines.Length; i++)
                {
                    sb.Append(baseIndentation);
                    sb.Append(" *  ");
                    sb.AppendLine(descLines[i]);
                }
            }
            else
            {
                sb.Append(baseIndentation);
                sb.AppendLine("/*");
            }

            sb.Append(baseIndentation);
            sb.AppendLine(" *");

            // Add diagram with markers
            sb.Append(baseIndentation);
            sb.AppendLine(" *  <!-- DIAGRAM_START -->");
            sb.Append(baseIndentation);
            sb.AppendLine(" *");

            var diagramLines = diagram.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in diagramLines)
            {
                // Skip the last empty line from split (after final newline)
                if (line == "" && diagramLines[diagramLines.Length - 1] == line)
                    continue;
                sb.Append(baseIndentation);
                sb.Append(" *  ");
                sb.AppendLine(line);
            }

            sb.Append(baseIndentation);
            sb.Append(" *  <!-- DIAGRAM_END -->");

            // Note: Don't add closing */ here - caller will add it
            return sb.ToString();
        }

        static List<(int x, int z)> ExtractDoorPositions(string callText)
        {
            var doorPositions = new List<(int x, int z)>();

            // Pattern to match door position entries: new DoorPosition { X = 10, Z = 9 }
            var doorPattern = new Regex(@"new DoorPosition \{ X = (\d+), Z = (\d+) \}");
            var matches = doorPattern.Matches(callText);

            foreach (Match match in matches)
            {
                int x = int.Parse(match.Groups[1].Value);
                int z = int.Parse(match.Groups[2].Value);
                doorPositions.Add((x, z));
            }

            return doorPositions;
        }
    }
}

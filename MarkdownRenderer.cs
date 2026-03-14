using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GamePrince
{
    public static class MarkdownRenderer
    {
        public static void Render(StackPanel container, string markdown, Action<int, bool>? onChecklistChanged = null)
        {
            container.Children.Clear();
            if (string.IsNullOrWhiteSpace(markdown)) return;

            string[] lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var checklistMatch = Regex.Match(line, @"^\s*-\s*\[([ xX])\]\s*(.*)");
                
                if (checklistMatch.Success)
                {
                    bool isChecked = checklistMatch.Groups[1].Value.ToLower() == "x";
                    string text = checklistMatch.Groups[2].Value;
                    int lineIndex = i;

                    var wp = new WrapPanel { Margin = new Thickness(0, 2, 0, 2) };
                    var cb = new CheckBox 
                    { 
                        IsChecked = isChecked, 
                        Margin = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    cb.Checked += (s, e) => onChecklistChanged?.Invoke(lineIndex, true);
                    cb.Unchecked += (s, e) => onChecklistChanged?.Invoke(lineIndex, false);

                    wp.Children.Add(cb);
                    wp.Children.Add(new TextBlock { 
                        Text = text, 
                        Foreground = Brushes.White, 
                        FontSize = 12, 
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Width = container.ActualWidth > 40 ? container.ActualWidth - 40 : 200
                    });
                    container.Children.Add(wp);
                }
                else if (line.Trim().StartsWith("-") || line.Trim().StartsWith("*"))
                {
                    // Bullet list
                    var tb = CreateRichTextBlock(line.Trim().Substring(1).Trim());
                    tb.Margin = new Thickness(15, 2, 0, 2);
                    container.Children.Add(tb);
                }
                else
                {
                    // Normal text
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var tb = CreateRichTextBlock(line);
                        tb.Margin = new Thickness(0, 2, 0, 5);
                        container.Children.Add(tb);
                    }
                }
            }
        }

        private static TextBlock CreateRichTextBlock(string text)
        {
            var tb = new TextBlock 
            { 
                Foreground = Brushes.White, 
                FontSize = 12, 
                TextWrapping = TextWrapping.Wrap 
            };

            // Simple regex for bold **text** and italic *text*
            string pattern = @"(\*\*.*?\*\*|\*.*?\*)";
            string[] parts = Regex.Split(text, pattern);

            foreach (var part in parts)
            {
                if (part.StartsWith("**") && part.EndsWith("**"))
                {
                    tb.Inlines.Add(new Run(part.Substring(2, part.Length - 4)) { FontWeight = FontWeights.Bold });
                }
                else if (part.StartsWith("*") && part.EndsWith("*"))
                {
                    tb.Inlines.Add(new Run(part.Substring(1, part.Length - 2)) { FontStyle = FontStyles.Italic });
                }
                else
                {
                    tb.Inlines.Add(new Run(part));
                }
            }

            return tb;
        }
    }
}

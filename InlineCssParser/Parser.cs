﻿using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InlineCssParser
{
    public class Parser
    {
        public string ParseHtml(string text, List<HtmlElement> elementList, TextDocument txtDoc, IVsStatusbar bar, ref uint cookie)
        {
            int pointer = 0;
            var startTagIndex = 0;
            var endTagIndex = 0;
            var endTagBefore = 0;
            uint completed = 0;
            uint total = (uint)text.Count(q => q == '<');

            while (text.Contains("; ") || text.Contains(": ")) //to fix style tag content
            {
                text = text.Replace("; ", ";").Replace(": ", ":");
            }

            startTagIndex = text.IndexOf('<', pointer);
            endTagIndex = text.IndexOf('>', pointer);

            try
            {
                txtDoc.Selection.StartOfDocument();
                while (pointer < text.Length && startTagIndex != -1 || endTagIndex != -1)
                {
                    completed++;
                    bar.Progress(ref cookie, 1, string.Empty, completed, total);
                    bar.SetText("Extracting inline styles ...");

                    #region to find current line (txtDoc.Selection.CurrentLine)

                    txtDoc.Selection.CharRight(false, endTagIndex - endTagBefore);
                    endTagBefore = endTagIndex;

                    #endregion

                    var elementText = text.Substring(startTagIndex + 1, (endTagIndex - (startTagIndex + 1)));
                    if (elementText.Contains("style=")) // '=' is really necessary
                    {
                        var parsedElement = elementText.Split(' ');
                        var elementName = parsedElement[0];
                        var elementId = string.Empty;
                        var elementStyle = string.Empty;
                        var elementClass = string.Empty;
                        var guid = Guid.NewGuid().ToString();

                        #region checking id attr

                        var idAttr = parsedElement.FirstOrDefault(q => q.Contains("id="));
                        if (idAttr != null)
                        {
                            elementId = idAttr.Replace("id=", string.Empty).Replace("\"", string.Empty);
                        }
                        #endregion

                        #region checking style attr

                        var styleAttr = parsedElement.FirstOrDefault(q => q.Contains("style="));
                        if (styleAttr != null)
                        {
                            elementStyle = styleAttr.Replace("style=", string.Empty).Replace("\"", string.Empty);
                        }
                        #endregion

                        #region check important style
                        var importantAttr = parsedElement.FirstOrDefault(q => q.Contains("!important"));
                        if (importantAttr != null)
                        {
                            elementStyle += string.Format(" {0}", importantAttr.Replace("\"", string.Empty).Trim());
                        }
                        #endregion

                        #region checking class attr

                        var classAttr = parsedElement.Any(q => q.Contains("class="));
                        if (classAttr)
                        {
                            //one class or more?
                            var classStart = elementText.IndexOf("\"", elementText.IndexOf("class"));
                            var classEnd = elementText.IndexOf("\"", classStart + 1);
                            var classText = elementText.Substring(classStart, (classEnd - classStart));
                            classText = classText.Replace(" ", " ."); // "class1 class2" - > "class1 .class2"
                            elementClass = classText.Replace("\"", string.Empty);
                        }

                        #endregion

                        text = text.Replace(elementText, guid);
                        pointer = text.IndexOf('>', text.IndexOf(guid)) + 1;

                        elementList.Add(new HtmlElement
                        {
                            Id = elementId,
                            Name = elementName,
                            Style = elementStyle,
                            Class = elementClass,
                            Guid = guid,
                            LineNumber = txtDoc.Selection.CurrentLine
                        });
                    }
                    else
                    {
                        pointer = endTagIndex + 1;
                    }

                    startTagIndex = text.IndexOf('<', pointer);
                    endTagIndex = text.IndexOf('>', pointer);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return text;
        }
    }
}
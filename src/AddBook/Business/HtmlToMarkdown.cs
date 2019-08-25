using AngleSharp.Dom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AddBook.Business
{
    public class HtmlToMarkdown
    {
        /// <summary>
        /// Convert HTML to Markdown.
        /// </summary>
        /// <param name="htmlNode">The HTML node to convert.</param>
        /// <returns>The markdown representation of the HTML content.</returns>
        public static string Convert(INode htmlNode)
        {
            if(htmlNode == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            ConvertContentToText(htmlNode.ChildNodes, sb);
            var text = sb.ToString();

            // strip leading white space and more than 2 consecutive line breaks
            return text.Trim();
        }

        private static void ConvertContentToText(INodeList node, StringBuilder outText)
        {
            foreach (var subnode in node)
            {
                ConvertToText(subnode, outText);
            }
        }

        private static void ConvertToText(INode node, StringBuilder outText)
        {
            switch (node.NodeType)
            {
                case NodeType.Comment: // don't output comments
                case NodeType.Document: // child node cannot be a document
                    break;

                case NodeType.Text:
                    var parentName = node.ParentElement.NodeName.ToLower();

                    // script, style and title text is ignored
                    if ((parentName == "script") || (parentName == "style") || parentName == "head"
                        || parentName == "title" || parentName == "meta")
                        break;

                    string html = node.TextContent;

                    if (parentName != "pre")
                    {
                        // get text with all characters remodes which are not visible in html
                        html = html.Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
                        var regEx = new Regex(@"\s+", RegexOptions.Compiled);
                        html = regEx.Replace(html, " ");
                    }

                    if (html.Length > 0)
                    {
                        outText.Append(html);
                    }
                    break;


                case NodeType.Element:
                    var element = (IElement)node;

                    switch (node.NodeName.ToLower())
                    {
                        case "p":
                            outText.AppendLine();
                            outText.AppendLine();
                            break;
                        case "br":
                        case "td":
                        case "ul":
                        case "ol":
                            outText.AppendLine();
                            break;
                        case "img":
                            // images
                            var imageContent = new List<string>();
                            if (element.Attributes["src"] != null && element.Attributes["src"].Value.Trim() != string.Empty)
                            {
                                imageContent.Add("[" + element.Attributes["src"].Value + "]");
                            }
                            if (element.Attributes["alt"] != null && element.Attributes["alt"].Value.Trim() != string.Empty)
                            {
                                imageContent.Add("[" + element.Attributes["alt"].Value + "]");
                            }
                            if (element.Attributes["title"] != null && element.Attributes["title"].Value.Trim() != string.Empty)
                            {
                                imageContent.Add("(\"" + element.Attributes["title"].Value + "\")");
                            }
                            outText.Append("[" + string.Join(" ", imageContent.ToArray()) + "] ");
                            break;
                        case "a":
                            // links
                            var linkContent = new List<string>();
                            if (element.Attributes["href"] != null && element.Attributes["href"].Value.Trim() != string.Empty)
                            {
                                linkContent.Add("[" + element.Attributes["href"].Value + "]");
                            }
                            if (element.Attributes["title"] != null && element.Attributes["title"].Value.Trim() != string.Empty)
                            {
                                linkContent.Add("(\"" + element.Attributes["title"].Value + "\")");
                            }
                            outText.Append(string.Join(" ", linkContent.ToArray()) + " ");
                            break;
                        case "hr":
                            outText.AppendLine();
                            outText.AppendLine("----------");
                            break;
                        case "b":
                        case "strong":
                            element.InnerHtml = "**" + element.InnerHtml.Trim() + "** ";
                            break;
                        case "i":
                        case "u":
                        case "em":
                            element.InnerHtml = "_" + element.InnerHtml.Trim() + "_ ";
                            break;
                        case "li":
                            element.InnerHtml = "* " + element.InnerHtml + "<br />";
                            break;
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            // headlines
                            outText.AppendLine();
                            outText.AppendLine();
                            element.InnerHtml = "#######".Substring(0, 7 - int.Parse(element.TagName.Substring(1))) + " " + element.InnerHtml + "<br />";
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentToText(node.ChildNodes, outText);
                    }
                    break;
            }
        }
    }
}
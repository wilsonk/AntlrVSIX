﻿namespace AntlrVSIX.ErrorTagger
{
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;
    using System.Windows.Media;

    public class ErrorFormatDefinition
    {
        public const string Suggestion = "AntlrVSIX - Suggestion";

        [Export(typeof(ErrorTypeDefinition))]
        [Name(Suggestion)]
        [DisplayName(Suggestion)]
        internal static ErrorTypeDefinition MessageDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(Suggestion)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class MessageFormat : EditorFormatDefinition
        {
            public MessageFormat()
            {
                DisplayName = Suggestion;
                ForegroundColor = (Color)ColorConverter.ConvertFromString("#CCc0c0c0");
            }
        }
    }
}

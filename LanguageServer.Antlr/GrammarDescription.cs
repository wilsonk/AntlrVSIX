﻿

namespace LanguageServer.Antlr
{
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using LanguageServer;
    using Symtab;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    class GrammarDescription : IGrammarDescription
    {
        public string Name { get; } = "Antlr";
        public System.Type Parser { get; } = typeof(ANTLRv4Parser);
        public System.Type Lexer { get; } = typeof(ANTLRv4Lexer);
        public ParserDetails CreateParserDetails(Workspaces.Document item)
        {
            return new AntlrParserDetails(item);
        }

        public void Parse(ParserDetails pd)
        {
            string ffn = pd.FullFileName;
            string code = pd.Code;

            IParseTree pt = null;

            // Set up Antlr to parse input grammar.
            byte[] byteArray = Encoding.UTF8.GetBytes(code);
            var ais = new AntlrInputStream(
                new StreamReader(
                    new MemoryStream(byteArray)).ReadToEnd());
            ais.name = ffn;
            var lexer = new ANTLRv4Lexer(ais);
            CommonTokenStream cts = new CommonTokenStream(lexer);
            var parser = new ANTLRv4Parser(cts);
            try
            {
                pt = parser.grammarSpec();
            }
            catch (Exception)
            {
                // Parsing error.
            }

            //StringBuilder sb = new StringBuilder();
            //TreeSerializer.ParenthesizedAST(pt, sb, "", cts);
            //string fn = System.IO.Path.GetFileName(ffn);
            //fn = "c:\\temp\\" + fn;
            //System.IO.File.WriteAllText(fn, sb.ToString());

            pd.TokStream = cts;
            pd.Parser = parser;
            pd.Lexer = lexer;
            pd.ParseTree = pt;
        }

        public void Parse(string code,
            out CommonTokenStream TokStream,
            out Parser Parser,
            out Lexer Lexer,
            out IParseTree ParseTree)
        {
            IParseTree pt = null;

            // Set up Antlr to parse input grammar.
            byte[] byteArray = Encoding.UTF8.GetBytes(code);
            var ais = new AntlrInputStream(
                new StreamReader(
                    new MemoryStream(byteArray)).ReadToEnd());
            var lexer = new ANTLRv4Lexer(ais);
            CommonTokenStream cts = new CommonTokenStream(lexer);
            var parser = new ANTLRv4Parser(cts);
            try
            {
                pt = parser.grammarSpec();
            }
            catch (Exception)
            {
                // Parsing error.
            }

            TokStream = cts;
            Parser = parser;
            Lexer = lexer;
            ParseTree = pt;
        }

        public Dictionary<IToken, int> ExtractComments(string code)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(code);
            CommonTokenStream cts_off_channel = new CommonTokenStream(
                new ANTLRv4Lexer(
                    new AntlrInputStream(
                        new StreamReader(
                            new MemoryStream(byteArray)).ReadToEnd())),
                ANTLRv4Lexer.OFF_CHANNEL);
            var new_list = new Dictionary<IToken, int>();
            var type = InverseMap[ClassificationNameComment];
            while (cts_off_channel.LA(1) != ANTLRv4Parser.Eof)
            {
                IToken token = cts_off_channel.LT(1);
                if (token.Type == ANTLRv4Lexer.BLOCK_COMMENT
                    || token.Type == ANTLRv4Lexer.LINE_COMMENT
                    || token.Type == ANTLRv4Lexer.DOC_COMMENT)
                {
                    new_list[token] = type;
                }
                cts_off_channel.Consume();
            }
            return new_list;
        }

        public string FileExtension { get; } = ".g4;.g";
        public string StartRule { get; } = "grammarSpec";

        public bool IsFileType(string ffn)
        {
            if (ffn == null) return false;
            var allowable_suffices = FileExtension.Split(';').ToList<string>();
            var suffix = Path.GetExtension(ffn).ToLower();
            foreach (var s in allowable_suffices)
                if (suffix == s)
                    return true;
            return false;
        }

        public Dictionary<IParseTree, ISymbol> GetSymbolTable()
        {
            return new Dictionary<IParseTree, ISymbol>();
        }


        /* Tagging and classification types. */
        private const string ClassificationNameTerminal = "Antlr - terminal";
        private const string ClassificationNameNonterminal = "Antlr - nonterminal";
        private const string ClassificationNameComment = "Antlr - comment";
        private const string ClassificationNameKeyword = "Antlr - keyword";
        private const string ClassificationNameLiteral = "Antlr - literal";
        private const string ClassificationNameMode = "Antlr - mode";
        private const string ClassificationNameChannel = "Antlr - channel";

        public string[] Map { get; } = new string[]
        {
            ClassificationNameNonterminal,
            ClassificationNameTerminal,
            ClassificationNameComment,
            ClassificationNameKeyword,
            ClassificationNameLiteral,
            ClassificationNameMode,
            ClassificationNameChannel,
        };

        public Dictionary<string, int> InverseMap { get; } = new Dictionary<string, int>()
        {
            { ClassificationNameNonterminal, 0 },
            { ClassificationNameTerminal, 1 },
            { ClassificationNameComment, 2 },
            { ClassificationNameKeyword, 3 },
            { ClassificationNameLiteral, 4 },
            { ClassificationNameMode, 5 },
            { ClassificationNameChannel, 6 },
        };

        /* Color scheme for the tagging. */
        public List<System.Drawing.Color> MapColor { get; } = new List<System.Drawing.Color>()
        {
            System.Drawing.Color.FromArgb(43, 145, 175), //ClassificationNameNonterminal
            System.Drawing.Color.Purple, //ClassificationNameTerminal
            System.Drawing.Color.FromArgb(0, 128, 0), //ClassificationNameComment
            System.Drawing.Color.FromArgb(0, 0, 255), //ClassificationNameKeyword
            System.Drawing.Color.FromArgb(163, 21, 21), //ClassificationNameLiteral
            System.Drawing.Color.Salmon, //ClassificationNameMode
            System.Drawing.Color.Coral, //ClassificationNameChannel
        };

        public List<System.Drawing.Color> MapInvertedColor { get; } = new List<System.Drawing.Color>()
        {
            System.Drawing.Color.LightPink,
            System.Drawing.Color.LightYellow,
            System.Drawing.Color.LightGreen,
            System.Drawing.Color.LightBlue,
            System.Drawing.Color.Red,
            System.Drawing.Color.LightSalmon,
            System.Drawing.Color.LightCoral,
        };

        public List<bool> CanFindAllRefs { get; } = new List<bool>()
        {
            true, // nonterminal
            true, // Terminal
            false, // comment
            false, // keyword
            true, // literal
            true, // mode
            true, // channel
        };

        public List<bool> CanRename { get; } = new List<bool>()
        {
            true, // nonterminal
            true, // Terminal
            false, // comment
            false, // keyword
            false, // literal
            true, // mode
            true, // channel
        };

        public List<bool> CanGotodef { get; } = new List<bool>()
        {
            true, // nonterminal
            true, // Terminal
            false, // comment
            false, // keyword
            false, // literal
            true, // mode
            true, // channel
        };

        public List<bool> CanGotovisitor { get; } = new List<bool>()
        {
            true, // nonterminal
            false, // Terminal
            false, // comment
            false, // keyword
            false, // literal
            false, // mode
            false, // channel
        };

        private static List<string> _antlr_keywords = new List<string>()
        {
            "options",
            "tokens",
            "channels",
            "import",
            "fragment",
            "lexer",
            "parser",
            "grammar",
            "protected",
            "public",
            "returns",
            "locals",
            "throws",
            "catch",
            "finally",
            "mode",
            "pushMode",
            "popMode",
            "type",
            "skip",
            "channel"
        };

        public List<Func<IGrammarDescription, Dictionary<IParseTree, IList<CombinedScopeSymbol>>, IParseTree, bool>> Identify { get; } = new List<Func<IGrammarDescription, Dictionary<IParseTree, IList<CombinedScopeSymbol>>, IParseTree, bool>>()
        {
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // nonterminal = 0
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is RefSymbol && ((RefSymbol)value).Def is NonterminalSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // terminal = 1
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is RefSymbol && ((RefSymbol)value).Def is TerminalSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            null, // comment = 2
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // keyword = 3
                {
                    TerminalNodeImpl nonterm = t as TerminalNodeImpl;
                    if (nonterm == null) return false;
                    var text = nonterm.GetText();
                    if (!_antlr_keywords.Contains(text)) return false;
                    return true;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // literal = 4
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    // Chicken/egg problem. Assume that literals are marked
                    // with the appropriate token type.
                    if (term.Symbol == null) return false;
                    if (!(term.Symbol.Type == ANTLRv4Parser.STRING_LITERAL
                        || term.Symbol.Type == ANTLRv4Parser.INT
                        || term.Symbol.Type == ANTLRv4Parser.LEXER_CHAR_SET))
                        return false;

                    // The token must be part of parserRuleSpec context.
                    for (var p = term.Parent; p != null; p = p.Parent)
                    {
                        if (p is ANTLRv4Parser.ParserRuleSpecContext ||
                            p is ANTLRv4Parser.LexerRuleSpecContext) return true;
                    }
                    return false;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // mode = 5
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is RefSymbol && ((RefSymbol)value).Def is ModeSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // channel = 6
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    var text = term.GetText();
                    if (_antlr_keywords.Contains(text)) return false;
                    if (! (term.Parent is ANTLRv4Parser.IdContext))
                        return false;
                    if (! (term.Parent.Parent is ANTLRv4Parser.LexerCommandExprContext))
                        return false;
                    if (! (term.Parent.Parent.Parent is ANTLRv4Parser.LexerCommandContext))
                        return false;
                    var p = term.Parent.Parent.Parent;
                    var id = p.GetChild(0).GetText();
                    if (id != "channel") return false;
                    return true;
                }
        };

        public List<Func<IGrammarDescription, Dictionary<IParseTree, IList<CombinedScopeSymbol>>, IParseTree, bool>> IdentifyDefinition { get; } = new List<Func<IGrammarDescription, Dictionary<IParseTree, IList<CombinedScopeSymbol>>, IParseTree, bool>>()
        {
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // nonterminal
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is NonterminalSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // terminal
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is TerminalSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            null, // comment
            null, // keyword
            null, // literal
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // mode
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is ModeSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                },
            (IGrammarDescription gd, Dictionary<IParseTree, IList<CombinedScopeSymbol>> st, IParseTree t) => // channel
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return false;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    st.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return false;
                    foreach (var value in list_value)
                    {
                        if (value is ChannelSymbol)
                        {
                            return true;
                        }
                    }
                    return false;
                }
        };

        public bool CanNextRule { get { return true; } }

        public bool DoErrorSquiggles { get { return true; } }

        public bool CanReformat { get { return true; } }

        public List<Func<ParserDetails, IParseTree, string>> PopUpDefinition { get; } =
            new List<Func<ParserDetails, IParseTree, string>>()
            {
                (ParserDetails pd, IParseTree t) => // nonterminal
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return null;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    var dir = System.IO.Path.GetDirectoryName(pd.Item.FullPath);
                    pd.Attributes.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return null;
                    StringBuilder sb = new StringBuilder();
                    foreach (var value in list_value)
                    {
                        if (value == null) continue;
                        var sym = value as ISymbol;
                        if (sym == null) continue;
                        if (sym is RefSymbol)
                        {
                            sym = sym.resolve();
                        }

                        if (sym is TerminalSymbol)
                            sb.Append("Terminal ");
                        else if (sym is NonterminalSymbol)
                            sb.Append("Nonterminal ");
                        else continue;
                        var def_file = sym.file;
                        if (def_file == null) continue;
                        var def_document = Workspaces.Workspace.Instance.FindDocument(def_file);
                        if (def_document == null) continue;
                        var def_pd = ParserDetailsFactory.Create(def_document);
                        if (def_pd == null) continue;
                        var fod = def_pd.Attributes.Where(
                                kvp => kvp.Value.Contains(value))
                            .Select(kvp => kvp.Key).FirstOrDefault();
                        if (fod == null) continue;
                        sb.Append("defined in ");
                        sb.Append(sym.file);
                        sb.AppendLine();
                        var node = fod;
                        for (; node != null; node = node.Parent)
                            if (node is ANTLRv4Parser.LexerRuleSpecContext ||
                                node is ANTLRv4Parser.ParserRuleSpecContext)
                                break;
                        if (node == null) continue;
                        Reconstruct.Doit(sb, node);
                    }

                    return sb.ToString();
                },
                (ParserDetails pd, IParseTree t) => // terminal
                {
                    TerminalNodeImpl term = t as TerminalNodeImpl;
                    if (term == null) return null;
                    Antlr4.Runtime.Tree.IParseTree p = term;
                    var dir = System.IO.Path.GetDirectoryName(pd.Item.FullPath);
                    pd.Attributes.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
                    if (list_value == null) return null;
                    StringBuilder sb = new StringBuilder();
                    foreach (var value in list_value)
                    {
                        if (value == null) continue;
                        var sym = value as ISymbol;
                        if (sym == null) continue;
                        if (sym is RefSymbol)
                        {
                            sym = sym.resolve();
                        }

                        if (sym is TerminalSymbol)
                            sb.Append("Terminal ");
                        else if (sym is NonterminalSymbol)
                            sb.Append("Nonterminal ");
                        else continue;
                        var def_file = sym.file;
                        if (def_file == null) continue;
                        var def_document = Workspaces.Workspace.Instance.FindDocument(def_file);
                        if (def_document == null) continue;
                        var def_pd = ParserDetailsFactory.Create(def_document);
                        if (def_pd == null) continue;
                        var fod = def_pd.Attributes.Where(
                                kvp => kvp.Value.Contains(value))
                            .Select(kvp => kvp.Key).FirstOrDefault();
                        if (fod == null) continue;
                        sb.Append("defined in ");
                        sb.Append(sym.file);
                        sb.AppendLine();
                        var node = fod;
                        for (; node != null; node = node.Parent)
                            if (node is ANTLRv4Parser.LexerRuleSpecContext ||
                                node is ANTLRv4Parser.ParserRuleSpecContext)
                                break;
                        if (node == null) continue;
                        Reconstruct.Doit(sb, node);
                    }

                    return sb.ToString();
                },
                null, // comment
                null, // keyword
                null, // literal
                null,
                null,
            };


    }
}

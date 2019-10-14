namespace LanguageServer.Antlr
{
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;
    using Symtab;

    public class Pass1Listener : ANTLRv4ParserBaseListener
    {
        private AntlrParserDetails _pd;

        public Pass1Listener(AntlrParserDetails pd)
        {
            _pd = pd;
        }

        public IParseTree NearestScope(IParseTree node)
        {
            for (; node != null; node = node.Parent)
            {
                if (_pd.Attributes.TryGetValue(node, out CombinedScopeSymbol value) && value is IScope)
                    return node;
            }
            return null;
        }

        public IScope GetScope(IParseTree node)
        {
            _pd.Attributes.TryGetValue(node, out CombinedScopeSymbol value);
            return value as IScope;
        }

        public override void EnterGrammarSpec([NotNull] ANTLRv4Parser.GrammarSpecContext context)
        {
            _pd.Attributes[context] = (CombinedScopeSymbol)_pd.RootScope;
        }

        public override void EnterParserRuleSpec([NotNull] ANTLRv4Parser.ParserRuleSpecContext context)
        {
            int i;
            for (i = 0; i < context.ChildCount; ++i)
            {
                if (!(context.GetChild(i) is TerminalNodeImpl)) continue;
                var c = context.GetChild(i) as TerminalNodeImpl;
                if (c.Symbol.Type == ANTLRv4Lexer.RULE_REF) break;
            }
            if (i == context.ChildCount) return;
            var rule_ref = context.GetChild(i) as TerminalNodeImpl;
            var id = rule_ref.GetText();
            ISymbol sym = new NonterminalSymbol(id, rule_ref.Symbol);
            _pd.RootScope.define(ref sym);
            var s = (CombinedScopeSymbol)sym;
            _pd.Attributes[context] = s;
            _pd.Attributes[context.GetChild(i)] = s;
        }

        public override void EnterLexerRuleSpec([NotNull] ANTLRv4Parser.LexerRuleSpecContext context)
        {
            int i;
            for (i = 0; i < context.ChildCount; ++i)
            {
                if (!(context.GetChild(i) is TerminalNodeImpl)) continue;
                var c = context.GetChild(i) as TerminalNodeImpl;
                if (c.Symbol.Type == ANTLRv4Lexer.TOKEN_REF) break;
            }
            if (i == context.ChildCount) return;
            var token_ref = context.GetChild(i) as TerminalNodeImpl;
            var id = token_ref.GetText();
            ISymbol sym = new TerminalSymbol(id, token_ref.Symbol);
            _pd.RootScope.define(ref sym);
            var s = (CombinedScopeSymbol)sym;
            _pd.Attributes[context] = s;
            _pd.Attributes[context.GetChild(i)] = s;
        }

        public override void EnterId([NotNull] ANTLRv4Parser.IdContext context)
        {
            if (context.Parent is ANTLRv4Parser.ModeSpecContext)
            {
                var term = context.GetChild(0) as TerminalNodeImpl;
                var id = term.GetText();
                ISymbol sym = new ModeSymbol(id, term.Symbol);
                _pd.RootScope.define(ref sym);
                var s = (CombinedScopeSymbol)sym;
                _pd.Attributes[context] = s;
                _pd.Attributes[context.GetChild(0)] = s;
            } else if (context.Parent is ANTLRv4Parser.IdListContext && context.Parent?.Parent is ANTLRv4Parser.ChannelsSpecContext)
            {
                var term = context.GetChild(0) as TerminalNodeImpl;
                var id = term.GetText();
                ISymbol sym = new ChannelSymbol(id, term.Symbol);
                _pd.RootScope.define(ref sym);
                var s = (CombinedScopeSymbol)sym;
                _pd.Attributes[context] = s;
                _pd.Attributes[term] = s;
            }
        }
    }
}

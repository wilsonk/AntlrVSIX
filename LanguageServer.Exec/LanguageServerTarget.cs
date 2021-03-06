﻿using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Workspaces;

namespace LanguageServer.Exec
{
    public class LanguageServerTarget
    {
        private readonly LSPServer server;
        private bool trace = true;
        private readonly Workspaces.Workspace _workspace;
        static readonly object _object = new object();

        public LanguageServerTarget(LSPServer server)
        {
            this.server = server;
            this._workspace = Workspaces.Workspace.Instance;
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- Initialize");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var capabilities = new ServerCapabilities();
            capabilities.TextDocumentSync = new TextDocumentSyncOptions();
            capabilities.TextDocumentSync.OpenClose = true;
            capabilities.TextDocumentSync.Change = TextDocumentSyncKind.Incremental;
            capabilities.TextDocumentSync.Save = new SaveOptions();
            capabilities.TextDocumentSync.Save.IncludeText = true;

            capabilities.HoverProvider = true;

            capabilities.CompletionProvider = new CompletionOptions();
            capabilities.CompletionProvider.ResolveProvider = false;
            capabilities.CompletionProvider.TriggerCharacters = new string[] { ",", "." };

            capabilities.ReferencesProvider = false;

            capabilities.DefinitionProvider = false;

            capabilities.TypeDefinitionProvider = false; // Does not make sense for Antlr.

            capabilities.ImplementationProvider = false; // Does not make sense for Antlr.

            capabilities.DocumentHighlightProvider = false;

            capabilities.DocumentSymbolProvider = false;

            capabilities.WorkspaceSymbolProvider = false;

            capabilities.DocumentFormattingProvider = false;

            capabilities.DocumentRangeFormattingProvider = false;

            capabilities.RenameProvider = false;

            var result = new InitializeResult();
            result.Capabilities = capabilities;
            string json = JsonConvert.SerializeObject(result);
            if (trace)
            {
                System.Console.Error.WriteLine("--> " + json);
            }
            return result;
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public async void InitializedName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- Initialized");
                System.Console.Error.WriteLine(arg.ToString());
            }
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public async System.Threading.Tasks.Task<JToken> ShutdownName()
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- Shutdown");
            }
            return null;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public async void ExitName()
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- Exit");
            }
            server.Exit();
        }

        // ======= WINDOW ========

        [JsonRpcMethod(Methods.WorkspaceDidChangeConfigurationName)]
        public async void WorkspaceDidChangeConfigurationName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- WorkspaceDidChangeConfiguration");
                System.Console.Error.WriteLine(arg.ToString());
            }
            //var parameter = arg.ToObject<DidChangeConfigurationParams>();
            //this.server.SendSettings(parameter);
        }

        [JsonRpcMethod(Methods.WorkspaceDidChangeWatchedFilesName)]
        public async void WorkspaceDidChangeWatchedFilesName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- WorkspaceDidChangeWatchedFiles");
                System.Console.Error.WriteLine(arg.ToString());
            }
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName)]
        public async System.Threading.Tasks.Task<JToken> WorkspaceSymbolName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- WorkspaceSymbol");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName)]
        public async System.Threading.Tasks.Task<JToken> WorkspaceExecuteCommandName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- WorkspaceExecuteCommand");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.WorkspaceApplyEditName)]
        public async System.Threading.Tasks.Task<JToken> WorkspaceApplyEditName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- WorkspaceApplyEdit");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        // ======= TEXT SYNCHRONIZATION ========

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public async void TextDocumentDidOpenName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDidOpen");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<DidOpenTextDocumentParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            server.SendDiagnostics(request.TextDocument.Uri.AbsoluteUri, "");
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public async void TextDocumentDidChangeName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDidChange");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<DidChangeTextDocumentParams>();
            var version = request.TextDocument.Version;
            var document = CheckDoc(request.TextDocument.Uri);
            lock (_object)
            {
                var pd = ParserDetailsFactory.Create(document);
                var code = pd.Code;
                var start_index = 0;
                var end_index = 0;
                foreach (var change in request.ContentChanges)
                {
                    var range = change.Range;
                    var length = change.RangeLength; // Why? range encodes start and end => length!
                    var text = change.Text;
                    {
                        var line = range.Start.Line;
                        var character = range.Start.Character;
                        start_index = LanguageServer.Module.GetIndex(line, character, document);
                    }
                    {
                        var line = range.End.Line;
                        var character = range.End.Character;
                        end_index = LanguageServer.Module.GetIndex(line, character, document);
                    }
                    var bs = LanguageServer.Module.GetLineColumn(start_index, document);
                    var be = LanguageServer.Module.GetLineColumn(end_index, document);
                    var original = code.Substring(start_index, end_index - start_index);
                    var n = code.Substring(0, start_index)
                            + text
                            + code.Substring(0 + start_index + end_index - start_index);
                    code = n;
                }
                document.Code = code;
                var to_do = LanguageServer.Module.Compile();
            }
        }

        [JsonRpcMethod(Methods.TextDocumentWillSaveName)]
        public async void TextDocumentWillSaveName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentWillSave");
                System.Console.Error.WriteLine(arg.ToString());
            }
            // Nothing to do--who cares because the server does not perform a save.
        }

        [JsonRpcMethod(Methods.TextDocumentWillSaveWaitUntilName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentWillSaveWaitUntilName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentWillSaveWaitUntil");
                System.Console.Error.WriteLine(arg.ToString());
            }
            // Nothing to do--who cares because the server does not perform a save, and
            // the server doesn't manufacture edit requests out of thin air.
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
        public async void TextDocumentDidSaveName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDidSave");
                System.Console.Error.WriteLine(arg.ToString());
            }
            // Nothing to do--who cares because the server does not perform a save.
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public async void TextDocumentDidCloseName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDidClose");
                System.Console.Error.WriteLine(arg.ToString());
            }
            // Nothing to do--who cares.
        }

        // ======= DIAGNOSTICS ========

        [JsonRpcMethod(Methods.TextDocumentPublishDiagnosticsName)]
        public async void TextDocumentPublishDiagnosticsName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentPublishDiagnostics");
                System.Console.Error.WriteLine(arg.ToString());
            }
        }

        // ======= LANGUAGE FEATURES ========

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentCompletionName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentCompletion");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<CompletionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var context = request.Context;
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            var res = LanguageServer.Module.Completion(index, document);
            List<CompletionItem> items = new List<CompletionItem>();
            //for (int i = 0; i < 10; i++)
            //{
            //    var item = new CompletionItem();
            //    item.Label = "Item " + i;
            //    item.InsertText = "Item" + i;
            //    item.Kind = (CompletionItemKind)(i % (Enum.GetNames(typeof(CompletionItemKind)).Length) + 1);
            //    items.Add(item);
            //}
            return items.ToArray();
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentCompletionResolveName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentCompletionResolve");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        Document CheckDoc(System.Uri uri)
        {
            var decoded = HttpUtility.UrlDecode(uri.AbsoluteUri);
            var file_name = new Uri(decoded).LocalPath;
            var document = _workspace.FindDocument(file_name);
            if (document == null)
            {
                document = new Workspaces.Document(file_name, file_name);
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(file_name))
                    {
                        // Read the stream to a string, and write the string to the console.
                        String str = sr.ReadToEnd();
                        document.Code = str;
                    }
                }
                catch (IOException e)
                {
                }
                var project = _workspace.FindProject("Misc");
                if (project == null)
                {
                    project = new Project("Misc", "Misc", "Misc");
                    _workspace.AddChild(project);
                }
                project.AddDocument(document);
                document.Changed = true;
                var pd = ParserDetailsFactory.Create(document);
                var to_do = LanguageServer.Module.Compile();
            }
            return document;
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName)]
        public async System.Threading.Tasks.Task<object> TextDocumentHoverName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentHover");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            QuickInfo quick_info = LanguageServer.Module.GetQuickInfo(index, document);
            if (quick_info == null) return null;
            var hover = new Hover();
            hover.Contents = new MarkupContent
            {
                Kind = MarkupKind.PlainText,
                Value = quick_info.Display
            };
            var index_start = quick_info.Range.Start.Value;
            var index_end = quick_info.Range.End.Value;
            var lcs = LanguageServer.Module.GetLineColumn(index_start, document);
            var lce = LanguageServer.Module.GetLineColumn(index_end, document);
            hover.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
            hover.Range.Start = new Position(lcs.Item1, lcs.Item2);
            hover.Range.End = new Position(lce.Item1, lce.Item2);
            System.Console.Error.WriteLine("returning " + quick_info.Display.ToString());
            return hover;
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentSignatureHelpName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentSignatureHelp");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        // TextDocumentDeclaration does not exist in Microsoft.VisualStudio.LanguageServer.Protocol 16.3.57
        // but does in version 3.14 of LSP.

        [JsonRpcMethod(Methods.TextDocumentDefinitionName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentDefinitionName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDefinition");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            IList<Location> found = LanguageServer.Module.FindDef(index, document);
            var locations = new List<object>();
            foreach (var f in found)
            {
                var location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                location.Uri = new Uri(f.Uri.FullPath);
                var def_document = _workspace.FindDocument(f.Uri.FullPath);
                location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(f.Range.Start.Value, def_document);
                var lce = LanguageServer.Module.GetLineColumn(f.Range.End.Value, def_document);
                location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                location.Range.End = new Position(lce.Item1, lce.Item2);
                locations.Add(location);
            }
            var result = locations.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentTypeDefinitionName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentTypeDefinitionName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentTypeDefinitionName");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            IList<Location> found = LanguageServer.Module.FindDef(index, document);
            var locations = new List<object>();
            foreach (var f in found)
            {
                var location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                location.Uri = new Uri(f.Uri.FullPath);
                var def_document = _workspace.FindDocument(f.Uri.FullPath);
                location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(f.Range.Start.Value, def_document);
                var lce = LanguageServer.Module.GetLineColumn(f.Range.End.Value, def_document);
                location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                location.Range.End = new Position(lce.Item1, lce.Item2);
                locations.Add(location);
            }
            var result = locations.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentImplementationName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentImplementationName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentImplementation");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            IList<Location> found = LanguageServer.Module.FindDef(index, document);
            var locations = new List<object>();
            foreach (var f in found)
            {
                var location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                location.Uri = new Uri(f.Uri.FullPath);
                var def_document = _workspace.FindDocument(f.Uri.FullPath);
                location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(f.Range.Start.Value, def_document);
                var lce = LanguageServer.Module.GetLineColumn(f.Range.End.Value, def_document);
                location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                location.Range.End = new Position(lce.Item1, lce.Item2);
                locations.Add(location);
            }
            var result = locations.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentReferencesName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentReferences");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            var found = LanguageServer.Module.FindRefsAndDefs(index, document);
            var locations = new List<object>();
            foreach (var f in found)
            {
                var location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                location.Uri = new Uri(f.Uri.FullPath);
                var def_document = _workspace.FindDocument(f.Uri.FullPath);
                location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(f.Range.Start.Value, def_document);
                var lce = LanguageServer.Module.GetLineColumn(f.Range.End.Value + 1, def_document);
                location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                location.Range.End = new Position(lce.Item1, lce.Item2);
                locations.Add(location);
            }
            var result = locations.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentDocumentHighlightName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDocumentHighlight");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<TextDocumentPositionParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            var found = LanguageServer.Module.FindRefsAndDefs(index, document);
            var locations = new List<object>();
            foreach (var f in found)
            {
                var location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                location.Uri = new Uri(f.Uri.FullPath);
                var def_document = _workspace.FindDocument(f.Uri.FullPath);
                location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(f.Range.Start.Value, def_document);
                var lce = LanguageServer.Module.GetLineColumn(f.Range.End.Value + 1, def_document);
                location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                location.Range.End = new Position(lce.Item1, lce.Item2);
                locations.Add(location);
            }
            var result = locations.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentDocumentSymbolName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDocumentSymbol");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<DocumentSymbolParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            IEnumerable<DocumentSymbol> r = LanguageServer.Module.Get(document);
            var symbols = new List<object>();
            foreach (var s in r)
            {
                var si = new SymbolInformation();
                if (s.kind == 0)
                    si.Kind = SymbolKind.Variable; // Nonterminal
                else if (s.kind == 1)
                    si.Kind = SymbolKind.Enum; // Terminal
                else if (s.kind == 2)
                    //si.Kind = 0; // Comment
                    continue;
                else if (s.kind == 3)
                    // si.Kind = 0; // Keyword
                    continue;
                else if (s.kind == 4)
                    // si.Kind = SymbolKind.Number; // Literal
                    continue;
                else if (s.kind == 5)
                    // si.Kind = 0; // Mode
                    continue;
                else if (s.kind == 6)
                    // si.Kind = SymbolKind.Enum; // Channel
                    continue;
                else
                    // si.Kind = 0; // Default.
                    continue;
                si.Name = s.name;
                si.Kind = (SymbolKind) si.Kind;
                si.Location = new Microsoft.VisualStudio.LanguageServer.Protocol.Location();
                si.Location.Uri = request.TextDocument.Uri;
                var lcs = LanguageServer.Module.GetLineColumn(s.range.Start.Value, document);
                var lce = LanguageServer.Module.GetLineColumn(s.range.End.Value, document);
                si.Location.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                si.Location.Range.Start = new Position(lcs.Item1, lcs.Item2);
                si.Location.Range.End = new Position(lce.Item1, lce.Item2);
                symbols.Add(si);
            }
            var result = symbols.ToArray();
            return result;
        }

        [JsonRpcMethod(Methods.TextDocumentCodeActionName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentCodeActionName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentCodeAction");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentCodeLensName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentCodeLensName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentCodeLens");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.CodeLensResolveName)]
        public async System.Threading.Tasks.Task<JToken> CodeLensResolveName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- CodeLensResolve");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentLinkName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentDocumentLinkName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentDocumentLink");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.DocumentLinkResolveName)]
        public async System.Threading.Tasks.Task<JToken> DocumentLinkResolveName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- DocumentLinkResolve");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentFormattingName)]
        public async System.Threading.Tasks.Task<object[]> TextDocumentFormattingName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentFormatting");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<DocumentFormattingParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var new_list = new List<Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit>();
            var changes = LanguageServer.Module.Reformat(document);
            int count = 0;
            foreach (var delta in changes)
            {
                var new_edit = new Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit();
                new_edit.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                var lcs = LanguageServer.Module.GetLineColumn(delta.range.Start.Value, document);
                var lce = LanguageServer.Module.GetLineColumn(delta.range.End.Value, document);
                new_edit.Range.Start = new Position(lcs.Item1, lcs.Item2);
                new_edit.Range.End = new Position(lce.Item1, lce.Item2);
                new_edit.NewText = delta.NewText;
                new_list.Add(new_edit);
                count++;
            }
            return new_list.ToArray();
        }

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentRangeFormattingName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentRangeFormatting");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentOnTypeFormattingName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentOnTypeFormatting");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName)]
        public async System.Threading.Tasks.Task<WorkspaceEdit> TextDocumentRenameName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentRename");
                System.Console.Error.WriteLine(arg.ToString());
            }
            var request = arg.ToObject<RenameParams>();
            var document = CheckDoc(request.TextDocument.Uri);
            var position = request.Position;
            var line = position.Line;
            var character = position.Character;
            var index = LanguageServer.Module.GetIndex(line, character, document);
            if (trace)
            {
                System.Console.Error.WriteLine("position index = " + index);
                var back = LanguageServer.Module.GetLineColumn(index, document);
                System.Console.Error.WriteLine("back to l,c = " + back.Item1 + "," + back.Item2);
            }
            var new_name = request.NewName;
            var changes = LanguageServer.Module.Rename(index, new_name, document);
            var edit = new WorkspaceEdit();
            int count = 0;
            var edit_changes_array = new Dictionary<string, Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit[]>();
            foreach (var pair in changes)
            {
                var doc = pair.Key;
                var uri = new Uri(doc);
                var val = pair.Value;
                var new_list = new List<Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit>();
                foreach (var v in val)
                {
                    var new_edit = new Microsoft.VisualStudio.LanguageServer.Protocol.TextEdit();
                    new_edit.Range = new Microsoft.VisualStudio.LanguageServer.Protocol.Range();
                    var lcs = LanguageServer.Module.GetLineColumn(v.range.Start.Value, document);
                    var lce = LanguageServer.Module.GetLineColumn(v.range.End.Value, document);
                    new_edit.Range.Start = new Position(lcs.Item1, lcs.Item2);
                    new_edit.Range.End = new Position(lce.Item1, lce.Item2);
                    new_edit.NewText = v.NewText;
                    new_list.Add(new_edit);
                    count++;
                }
                edit_changes_array.Add(uri.ToString(), new_list.ToArray());
            }
            edit.Changes = edit_changes_array;
            return edit;
        }

        [JsonRpcMethod(Methods.TextDocumentFoldingRangeName)]
        public async System.Threading.Tasks.Task<JToken> TextDocumentFoldingRangeName(JToken arg)
        {
            if (trace)
            {
                System.Console.Error.WriteLine("<-- TextDocumentFoldingRange");
                System.Console.Error.WriteLine(arg.ToString());
            }
            return null;
        }


        public string GetText()
        {
            return string.IsNullOrWhiteSpace(this.server.CustomText) ? "custom text from language server target" : this.server.CustomText;
        }
    }
}

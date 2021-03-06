﻿namespace Workspaces
{
    using System.Collections.Generic;
    using System.Linq;

    public class Workspace : Container
    {
        static Workspace _instance;
        string _name;
        string _ffn;
        List<Container> _contents = new List<Container>();

        public static Workspace Initialize(string name, string ffn)
        {
            if (_instance != null) return _instance;
            var i = Instance;
            i._name = name;
            i._ffn = ffn;
            return i;
        }

        public static Workspace Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Workspace();
                return _instance;
            }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string FFN
        {
            get { return _ffn; }
            set { _ffn = value; }
        }

        public IEnumerable<Container> Children
        {
            get { return _contents; }
        }

        public override Container AddChild(Container doc)
        {
            _contents.Add(doc);
            doc.Parent = this;
            return doc;
        }

        public override Document FindDocument(string ffn)
        {
            if (ffn == null) return null;
            foreach (var doc in _contents)
            {
                var found = doc.FindDocument(ffn);
                if (found != null) return found;
            }
            return null;
        }

        public IEnumerable<Document> AllDocuments()
        {
            HashSet<Container> visited = new HashSet<Container>();
            Stack<Container> stack = new Stack<Container>();
            stack.Push(this);
            while (stack.Any())
            {
                var current = stack.Pop();
                if (visited.Contains(current)) continue;
                visited.Add(current);
                if (current is Document)
                {
                    yield return current as Document;
                }
                else
                {
                    foreach (var c in this._contents)
                    {
                        stack.Push(c);
                    }
                }
            }
        }

        public override Project FindProject(string ffn)
        {
            foreach (var doc in _contents)
            {
                var found = doc.FindProject(ffn);
                if (found != null) return found;
            }
            return null;
        }

        public override Project FindProject(string canonical_name, string name, string ffn)
        {
            foreach (var doc in _contents)
            {
                var found = doc.FindProject(canonical_name, name, ffn);
                if (found != null) return found;
            }
            return null;
        }
    }
}

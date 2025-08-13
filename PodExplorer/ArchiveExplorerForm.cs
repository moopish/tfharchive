using tfharchive.archive.data;

namespace PodExplorer
{
    public sealed class ArchiveExplorerForm : Form
    {
        private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
        private readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false };
        private readonly ListView _list = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
        private readonly StatusStrip _status = new();
        private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStrip _top = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripLabel _searchLabel = new("Search:");
        private readonly ToolStripTextBox _searchBox = new() { AutoSize = false, Width = 200 };
        private readonly ToolStripButton _clearBtn = new("Clear");

        private readonly List<ArchiveEntry> _entries = [];
        private List<ArchiveEntry> _view = [];
        private string _selectedDir = "";               // normalized directory filter ("" = root/all)
        private int _sortCol = 0;                       // default sort by Name column
        private bool _sortAsc = true;

        public event EventHandler<ArchiveEntryEventArgs>? EntryActivated;

        public ArchiveExplorerForm() : this([]) { }

        public ArchiveExplorerForm(IEnumerable<ArchiveEntry> entries)
        {
            Text = "Archive Explorer";
            Width = 1000;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;

            // Top toolbar
            _top.Items.AddRange([_searchLabel, _searchBox, _clearBtn]);

            // Left: Tree
            _split.Panel1.Controls.Add(_tree);

            // Right: List
            _list.Columns.Add("Directory", 200);
            _list.Columns.Add("Name", 250);
            _list.Columns.Add("Extra", 180);
            _list.Columns.Add("Size", 100, HorizontalAlignment.Right);
            _list.Columns.Add("Offset", 120, HorizontalAlignment.Right);
            _split.Panel2.Controls.Add(_list);

            // Status bar
            _status.Items.Add(_statusLabel);

            Controls.Add(_split);
            Controls.Add(_top);
            Controls.Add(_status);
            _top.Dock = DockStyle.Top;
            _status.Dock = DockStyle.Bottom;

            // Events
            _tree.AfterSelect += (_, __) => { _selectedDir = NormalizeDir(_tree.SelectedNode?.FullPath == "(root)" ? "" : _tree.SelectedNode?.FullPath?.Replace("(root)\\", "") ?? ""); RefreshList(); };
            _searchBox.TextChanged += (_, __) => RefreshList();
            _clearBtn.Click += (_, __) => _searchBox.Clear();
            _list.ColumnClick += (_, e) => { ToggleSort(e.Column); RefreshList(); };
            _list.DoubleClick += OnItemActivate;

            SetEntries(entries);
        }

        public void SetEntries(IEnumerable<ArchiveEntry> entries)
        {
            _entries.Clear();
            _entries.AddRange(entries.Select(e => e with { Directory = NormalizeDir(e.Directory ?? "") }));
            BuildTree();
            _selectedDir = ""; // default to root
            SelectRoot();
            RefreshList();
        }

        private void BuildTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();

            // Root node
            var root = new TreeNode("(root)");
            _tree.Nodes.Add(root);

            // Build nested nodes for all unique directories
            foreach (var dir in _entries.Select(e => e.Directory).Distinct().OrderBy(d => d))
            {
                AddDirectoryPath(root, dir);
            }

            root.Expand();
            _tree.EndUpdate();
        }

        private static string NormalizeDir(string d)
        {
            if (string.IsNullOrEmpty(d)) return "";
            return d.Replace('/', '\\').Trim('\\');
        }

        private static void AddDirectoryPath(TreeNode root, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var parts = path.Split(['\\'], StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            foreach (var part in parts)
            {
                var next = current.Nodes.Cast<TreeNode>().FirstOrDefault(n => string.Equals(n.Text, part, StringComparison.OrdinalIgnoreCase));
                if (next == null)
                {
                    next = new TreeNode(part);
                    current.Nodes.Add(next);
                }
                current = next;
            }
        }

        private void SelectRoot()
        {
            if (_tree.Nodes.Count > 0) _tree.SelectedNode = _tree.Nodes[0];
        }

        private void ToggleSort(int col)
        {
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
        }

        private void RefreshList()
        {
            var q = _entries.AsEnumerable();

            // Directory filter
            if (!string.IsNullOrEmpty(_selectedDir))
            {
                // Match exact dir or child dirs
                string dirPrefix = _selectedDir + "\\";
                q = q.Where(e =>
                    string.Equals(e.Directory, _selectedDir, StringComparison.OrdinalIgnoreCase) ||
                    (e.Directory?.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Search filter
            var term = _searchBox.Text;
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();
                q = q.Where(e =>
                    (!string.IsNullOrEmpty(e.Name) && e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(e.Directory) && e.Directory.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(e.Extra) && e.Extra.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            // Sorting
            q = (_sortCol, _sortAsc) switch
            {
                (0, true) => q.OrderBy(e => e.Directory, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (0, false) => q.OrderByDescending(e => e.Directory, StringComparer.OrdinalIgnoreCase).ThenByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (1, true) => q.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (1, false) => q.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (2, true) => q.OrderBy(e => e.Extra, StringComparer.OrdinalIgnoreCase),
                (2, false) => q.OrderByDescending(e => e.Extra, StringComparer.OrdinalIgnoreCase),
                (3, true) => q.OrderBy(e => e.Size).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (3, false) => q.OrderByDescending(e => e.Size).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (4, true) => q.OrderBy(e => e.Offset),
                (4, false) => q.OrderByDescending(e => e.Offset),
                _ => q.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            };

            _view = [.. q];
            _list.BeginUpdate();
            _list.Items.Clear();

            foreach (var e in _view)
            {
                var item = new ListViewItem(e.Directory);
                item.SubItems.Add(e.Name ?? "");
                item.SubItems.Add(e.Extra ?? "");
                item.SubItems.Add(FormatSize(e.Size));
                item.SubItems.Add($"0x{e.Offset:X8}");
                item.Tag = e;
                _list.Items.Add(item);
            }

            _list.EndUpdate();
            _statusLabel.Text = $"{_view.Count} item(s)  |  {_entries.Count} total";
        }

        private static string FormatSize(int size)
        {
            // Simple human-readable size
            string[] units = ["B", "KB", "MB", "GB"];
            double s = size;
            int u = 0;
            while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
            return (u == 0) ? $"{size} {units[u]}" : $"{s:0.##} {units[u]}";
        }

        private void OnItemActivate(object? sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;
            var entry = _list.SelectedItems[0].Tag as ArchiveEntry;
            if (entry is null) return;

            EntryActivated?.Invoke(this, new ArchiveEntryEventArgs(entry));
        }

        // simple EventArgs wrapper
        public sealed class ArchiveEntryEventArgs : EventArgs
        {
            public ArchiveEntry Entry { get; }
            public ArchiveEntryEventArgs(ArchiveEntry entry) => Entry = entry;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet
{
    #region Entry Model

    public abstract class Entry
    {
        public string Name { get; set; }
        public bool IsNew { get; set; }
        public bool IsModified { get; set; }
    }

    public class SolutionEntry : Entry
    {
        public ObservableCollection<DocumentEntry> Documents { get; set; }
    }

    public class DocumentEntry : Entry
    {
        public SolutionEntry Solution { get; set; }
        public ObservableCollection<PageEntry> Pages { get; set; }
    }

    public class PageEntry : Entry
    {
        public DocumentEntry Document { get; set; }
        public string Content { get; set; }
    }

    public interface IEntryController
    {
        void Set(string text);
        string Get();
        void Export(string text);
        void Export(IEnumerable<string> texts);
    }

    #endregion

    #region Entry Serializer

    public static class EntrySerializer
    {
        #region Fields

        private static char[] entryNameSeparator = { '/' };

        #endregion

        #region Add

        public static void AddDocumentEntry(ZipArchive zip, string document)
        {
            var name = string.Concat(document, '/');
            var entry = zip.CreateEntry(name);
        }

        public static void AddPageEntry(ZipArchive zip, string document, string page, string content)
        {
            var name = string.Concat(document, '/', page);
            var entry = zip.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }

        #endregion

        #region Serialize

        public static void Serialize(SolutionEntry solution, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    foreach (var document in solution.Documents)
                    {
                        if (document.Pages.Count <= 0)
                        {
                            AddDocumentEntry(zip, document.Name);
                        }

                        foreach (var page in document.Pages)
                        {
                            AddPageEntry(zip, document.Name, page.Name, page.Content);
                        }
                    }
                }
            }
        }

        public static void Serialize(SolutionEntry solution)
        {
            Serialize(solution, string.Concat(solution.Name, ".zip"));
        }

        #endregion

        #region Deserialize

        public static SolutionEntry Deserialize(string path)
        {
            string solutionName = System.IO.Path.GetFileNameWithoutExtension(path);

            var dict = new Dictionary<string, List<Tuple<string, string>>>();
            var solution = new SolutionEntry() { Name = solutionName, Documents = new ObservableCollection<DocumentEntry>() };

            using (var zip = ZipFile.Open(path, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    var e = entry.FullName.Split(entryNameSeparator);
                    if (e.Length == 1)
                    {
                        string key = e[0];

                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, new List<Tuple<string, string>>());
                        }
                    }
                    else if (e.Length == 2)
                    {
                        string key = e[0];
                        string data = e[1];
                        string content = null;

                        using (var reader = new StreamReader(entry.Open()))
                        {
                            content = reader.ReadToEnd();
                        }

                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, new List<Tuple<string, string>>());
                        }

                        dict[key].Add(new Tuple<string, string>(data, content));
                    }
                }
            }

            foreach (var item in dict)
            {
                var document = EntryFactory.CreateDocument(solution, item.Key);
                solution.Documents.Add(document);
                foreach (var tuple in item.Value)
                {
                    var page = EntryFactory.CreatePage(document, tuple.Item2, tuple.Item1);
                    document.Pages.Add(page);
                }
            }

            return solution;
        }

        #endregion
    }

    #endregion

    #region Entry Controller

    public static class EntryController
    {
        #region Page

        public static PageEntry AddPage(DocumentEntry document, string content)
        {
            var page = EntryFactory.CreatePage(document, content);
            document.Pages.Add(page);
            return page;
        }

        public static PageEntry AddPageBefore(DocumentEntry document, PageEntry beofore, string content)
        {
            var page = EntryFactory.CreatePage(document, content);
            int index = document.Pages.IndexOf(beofore);
            document.Pages.Insert(index, page);
            return page;
        }

        public static PageEntry AddPageAfter(DocumentEntry document, PageEntry after, string content)
        {
            var page = EntryFactory.CreatePage(document, content);
            int index = document.Pages.IndexOf(after);
            document.Pages.Insert(index + 1, page);
            return page;
        }

        public static void AddPageAfter(object item)
        {
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var document = page.Document;
                if (document != null)
                {
                    AddPageAfter(document, page, "");
                }
            }
        }

        public static void AddPageBefore(object item)
        {
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var document = page.Document;
                if (document != null)
                {
                    AddPageBefore(document, page, "");
                }
            }
        }

        public static void DuplicatePage(object item)
        {
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var document = page.Document;
                if (document != null)
                {
                    AddPage(document, page.Content);
                }
            }
        }

        public static void RemovePage(object item)
        {
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var document = page.Document;
                if (document != null)
                {
                    document.Pages.Remove(page);
                }
            }
        }

        #endregion

        #region Document

        public static DocumentEntry AddDocumentBefore(SolutionEntry solution, DocumentEntry after)
        {
            var document = EntryFactory.CreateDocument(solution);
            int index = solution.Documents.IndexOf(after);
            solution.Documents.Insert(index, document);
            return document;
        }

        public static DocumentEntry AddDocumentAfter(SolutionEntry solution, DocumentEntry after)
        {
            var document = EntryFactory.CreateDocument(solution);
            int index = solution.Documents.IndexOf(after);
            solution.Documents.Insert(index + 1, document);
            return document;
        }

        public static DocumentEntry AddDocument(SolutionEntry solution)
        {
            var document = EntryFactory.CreateDocument(solution);
            solution.Documents.Add(document);
            return document;
        }

        public static void DocumentAddPage(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                AddPage(document, "");
            }
        }

        public static void AddDocumentAfter(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                var solution = document.Solution;
                if (solution != null)
                {
                    AddDocumentAfter(solution, document);
                }
            }
        }

        public static void AddDocumentBefore(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                var solution = document.Solution;
                if (solution != null)
                {
                    AddDocumentBefore(solution, document);
                }
            }
        }

        public static void DulicateDocument(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                var solution = document.Solution;
                if (solution != null)
                {
                    var duplicate = AddDocument(solution);
                    foreach (var page in document.Pages)
                    {
                        AddPage(duplicate, page.Content);
                    }
                }
            }
        }

        public static void RemoveDocument(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                var solution = document.Solution;
                if (solution != null)
                {
                    solution.Documents.Remove(document);
                }
            }
        }

        #endregion
    }

    #endregion

    #region Entry Factory

    public static class EntryFactory
    {
        #region Empty

        public static void CreateEmpty(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    EntrySerializer.AddPageEntry(zip, "Document0", "Page", "");
                }
            }
        }

        #endregion

        #region Create

        public static PageEntry CreatePage(DocumentEntry document, string content, string name = null)
        {
            var page = new PageEntry()
            {
                Name = name == null ? "Page" : name,
                Content = content,
                Document = document
            };
            return page;
        }

        public static DocumentEntry CreateDocument(SolutionEntry solution, string name = null)
        {
            var document = new DocumentEntry()
            {
                Name = name == null ? string.Concat("Document", solution.Documents.Count) : name,
                Pages = new ObservableCollection<PageEntry>(),
                Solution = solution
            };
            return document;
        }

        #endregion
    }

    #endregion
}
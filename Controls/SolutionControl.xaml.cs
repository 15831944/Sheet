﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sheet
{
    public partial class SolutionControl : UserControl
    {
        #region Properties

        public IEntryEditor EntryEditor { get; set; }

        #endregion

        #region Constructor

        public SolutionControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Entries Editor

        private void EntryChanged(object oldValue, object newValue)
        {
            if (newValue != null && newValue is DocumentEntry)
            {
                if (EntryEditor != null)
                {
                    if (oldValue != null && oldValue is PageEntry)
                    {
                        UpdatePage(oldValue);
                    }

                    EmptyPage();
                }
            }
            else if (newValue != null && newValue is PageEntry)
            {
                if (EntryEditor != null)
                {
                    if (oldValue != null && oldValue is PageEntry)
                    {
                        UpdatePage(oldValue);
                    }

                    SetPage(newValue);
                }
            }
            else
            {
                if (EntryEditor != null)
                {
                    EmptyPage();
                }
            }
        }

        private void EmptyPage()
        {
            EntryEditor.Set(null);
        }

        private void SetPage(object newValue)
        {
            var newPage = newValue as PageEntry;
            EntryEditor.Set(newPage.Content);
        }

        private void UpdatePage(object oldValue)
        {
            var oldPage = oldValue as PageEntry;
            var text = EntryEditor.Get();
            oldPage.Content = text;
        }

        public void UpdateSelectedPage()
        {
            var item = SolutionTree.SelectedItem;
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var text = EntryEditor.Get();
                page.Content = text;
            }
        }

        #endregion

        #region Solution Editor

        private static PageEntry CreatePage(DocumentEntry document, string content)
        {
            var page = new PageEntry()
            {
                Name = "Page",
                Content = content,
                Document = document
            };
            return page;
        }

        private static DocumentEntry CreateDocument(SolutionEntry solution)
        {
            var document = new DocumentEntry()
            {
                Name = string.Concat("Document", solution.Documents.Count),
                Pages = new ObservableCollection<PageEntry>(),
                Solution = solution
            };
            return document;
        }

        private PageEntry AddPage(DocumentEntry document, string content)
        {
            var page = CreatePage(document, content);
            document.Pages.Add(page);
            return page;
        }

        private PageEntry AddPageBefore(DocumentEntry document, PageEntry beofore, string content)
        {
            var page = CreatePage(document, content);
            int index = document.Pages.IndexOf(beofore);
            document.Pages.Insert(index, page);
            return page;
        }

        private PageEntry AddPageAfter(DocumentEntry document, PageEntry after, string content)
        {
            var page = CreatePage(document, content);
            int index = document.Pages.IndexOf(after);
            document.Pages.Insert(index + 1, page);
            return page;
        }

        private DocumentEntry AddDocumentBefore(SolutionEntry solution, DocumentEntry after)
        {
            var document = CreateDocument(solution);
            int index = solution.Documents.IndexOf(after);
            solution.Documents.Insert(index, document);
            return document;
        }

        private DocumentEntry AddDocumentAfter(SolutionEntry solution, DocumentEntry after)
        {
            var document = CreateDocument(solution);
            int index = solution.Documents.IndexOf(after);
            solution.Documents.Insert(index + 1, document);
            return document;
        }

        private DocumentEntry AddDocument(SolutionEntry solution)
        {
            var document = CreateDocument(solution);
            solution.Documents.Add(document);
            return document;
        }

        private void AddPageAfter(object item)
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

        private void AddPageBefore(object item)
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

        private void DuplicatePage(object item)
        {
            if (item != null && item is PageEntry)
            {
                var page = item as PageEntry;
                var document = page.Document;
                if (document != null)
                {
                    AddPageAfter(document, page, page.Content);
                }
            }
        }

        private void RemovePage(object item)
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

        private void ExportPage(object item)
        {
            if (EntryEditor != null)
            {
                if (item != null && item is PageEntry)
                {
                    var page = item as PageEntry;
                    EntryEditor.Export(page.Content);
                }
            }
        } 

        private void DocumentAddPage(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                AddPage(document, "");
            }
        }

        private void AddDocumentAfter(object item)
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

        private void AddDocumentBefore(object item)
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

        private void DulicateDocument(object item)
        {
            if (item != null && item is DocumentEntry)
            {
                var document = item as DocumentEntry;
                var solution = document.Solution;
                if (solution != null)
                {
                    var duplicate = AddDocumentAfter(solution, document);
                    foreach (var page in document.Pages)
                    {
                        AddPage(duplicate, page.Content);
                    }
                }
            }
        }

        private void RemoveDocument(object item)
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

        private void ExportDocument(object item)
        {
            if (EntryEditor != null)
            {
                if (item != null && item is DocumentEntry)
                {
                    var document = item as DocumentEntry;
                    var texts = document.Pages.Select(x => x.Content);
                    EntryEditor.Export(texts);
                }
            }
        } 

        #endregion

        #region TreeView Events

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            EntryChanged(e.OldValue, e.NewValue);
        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item != null)
            {
                item.IsSelected = true;
                item.BringIntoView();
                e.Handled = true;
            }
        }

        #endregion

        #region Page Menu Events

        private void PageInsertBefore_Click(object sender, RoutedEventArgs e)
        {
            AddPageBefore(SolutionTree.SelectedItem);
        }

        private void PageInsertAfter_Click(object sender, RoutedEventArgs e)
        {
            AddPageAfter(SolutionTree.SelectedItem);
        }

        private void PageDuplicate_Click(object sender, RoutedEventArgs e)
        {
            DuplicatePage(SolutionTree.SelectedItem);
        }

        private void PageRemove_Click(object sender, RoutedEventArgs e)
        {
            RemovePage(SolutionTree.SelectedItem);
        }

        private void PageExport_Click(object sender, RoutedEventArgs e)
        {
            ExportPage(SolutionTree.SelectedItem);
        }

        #endregion

        #region Document Menu Events

        private void DocumentAddPage_Click(object sender, RoutedEventArgs e)
        {
            DocumentAddPage(SolutionTree.SelectedItem);
        }

        private void DocumentInsertBofre_Click(object sender, RoutedEventArgs e)
        {
            AddDocumentBefore(SolutionTree.SelectedItem);
        }

        private void DocumentInsertAfter_Click(object sender, RoutedEventArgs e)
        {
            AddDocumentAfter(SolutionTree.SelectedItem);
        }

        private void DocumentDuplicate_Click(object sender, RoutedEventArgs e)
        {
            DulicateDocument(SolutionTree.SelectedItem);
        }

        private void DocumentRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveDocument(SolutionTree.SelectedItem);
        }

        private void DocumentExport_Click(object sender, RoutedEventArgs e)
        {
            ExportDocument(SolutionTree.SelectedItem);
        }

        #endregion

        #region Tree Menu Events

        private void TreeAddDocument_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && DataContext is SolutionEntry)
            {
                AddDocument(DataContext as SolutionEntry);
            }
        }

        #endregion
    }
}
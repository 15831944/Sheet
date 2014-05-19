﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet
{
    #region IHistoryController

    public interface IHistoryController
    {
        void Register(string message);
        void Reset();
        void Undo();
        void Redo();
    } 

    #endregion

    #region ILibraryController

    public interface ILibraryController
    {
        BlockItem GetSelected();
        void SetSelected(BlockItem block);
        IEnumerable<BlockItem> GetSource();
        void SetSource(IEnumerable<BlockItem> source);
    } 

    #endregion

    #region IDatabaseController

    public interface IDatabaseController
    {
        string[] Get(int index);
        bool Update(int index, string[] item);
        int Add(string[] item);
    }

    #endregion

    #region ITextController

    public interface ITextController
    {
        void Set(Action<string> ok, Action cancel, string title, string label, string text);
    }

    #endregion

    #region IPageController

    public interface IPageController
    {
        void SetPage(string text);
        string GetPage();
        void ExportPage(string text);
        void ExportPages(IEnumerable<string> texts);
        BlockItem SerializePage();
        void DeserializePage(BlockItem page);
        void ResetPage();
        void ResetPageContent();
    }

    #endregion
}

﻿using Sheet.Block.Core;
using Sheet.Block.Model;
using Sheet.Entry.Model;
using Sheet.Item.Model;
using Sheet.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet.Controller.Core
{
    public interface ISheetController
    {
        // Properties
        SheetState State { get; set; }

        // Init
        void Init();

        // Clipboard
        void CutText();
        void CopyText();
        void PasteText();
        void CutJson();
        void CopyJson();
        void PasteJson();

        // Delete
        void Delete(IBlock block);
        void Delete();

        // Select All
        void SelecteAll();

        // Deselect All
        void DeselectAll();

        // Toggle Fill
        void ToggleFill();

        // Insert Mode
        void CreateBlock();
        void BreakBlock();

        // Move Mode
        void MoveUp();
        void MoveDown();
        void MoveLeft();
        void MoveRight();

        // Pan & Zoom Mode
        void SetAutoFitSize(double finalWidth, double finalHeight);
        void AdjustBackThickness(double zoom);
        void AdjustPageThickness(double zoom);

        // Data Binding
        bool BindDataToBlock(ImmutablePoint p, DataItem dataItem);
        bool BindDataToBlock(IBlock block, DataItem dataItem);
        void TryToBindData(ImmutablePoint p, DataItem dataItem);

        // New Page
        void NewPage();

        // Open Page
        Task OpenTextPage(string path);
        Task OpenJsonPage(string path);
        void OpenPage();

        // Save Page
        void SaveTextPage(string path);
        void SaveJsonPage(string path);
        void SavePage();

        // Export Page
        void Export(IEnumerable<BlockItem> blocks);
        void Export(SolutionEntry solution);
        void ExportPage();

        // Library
        void Insert(ImmutablePoint p);
        IBlock Insert(BlockItem blockItem, ImmutablePoint p, bool select);
        Task LoadLibrary(string path);
        void LoadLibrary();

        // Input
        void LeftDown(InputArgs args);
        void LeftUp(InputArgs args);
        void Move(InputArgs args);
        void RightDown(InputArgs args);
        void RightUp(InputArgs args);
        void Wheel(int delta, ImmutablePoint position);
        void Down(InputArgs args);

        // Page
        void SetPage(string text);
        string GetPage();
        void ExportPage(string text);
        void ExportPages(IEnumerable<string> texts);
        BlockItem SerializePage();
        void DeserializePage(BlockItem page);
        void ResetPage();
        void ResetPageContent();

        // Plugins
        void InvertSelectedLineStart();
        void InvertSelectedLineEnd();
    }
}
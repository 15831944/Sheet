﻿using Sheet.Block;
using Sheet.Block.Core;
using Sheet.Block.Model;
using Sheet.Entry.Model;
using Sheet.Item;
using Sheet.Item.Model;
using Sheet.Plugins;
using Sheet.Controller.Core;
using Sheet.UI.Views;
using Sheet.Util.Core;
using Sheet.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sheet.Controller.Modes;

namespace Sheet.Controller
{
    public class SheetController : ISheetController
    {
        #region IoC

        private readonly IServiceLocator _serviceLocator;
        private readonly IBlockController _blockController;
        private readonly IBlockFactory _blockFactory;
        private readonly IBlockSerializer _blockSerializer;
        private readonly IBlockHelper _blockHelper;
        private readonly IItemController _itemController;
        private readonly IItemSerializer _itemSerializer;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IClipboard _clipboard;
        private readonly IBase64 _base64;
        private readonly IPointController _pointController;
        private readonly IPageFactory _pageFactory;

        private readonly SheetLineMode _lineMode;
        private readonly SheetRectangleMode _rectangleMode;
        private readonly SheetEllipseMode _ellipseMode;

        public SheetController(IServiceLocator serviceLocator)
        {
            this._serviceLocator = serviceLocator;
            this._blockController = serviceLocator.GetInstance<IBlockController>();
            this._blockFactory = serviceLocator.GetInstance<IBlockFactory>();
            this._blockSerializer = serviceLocator.GetInstance<IBlockSerializer>();
            this._blockHelper = serviceLocator.GetInstance<IBlockHelper>();
            this._itemController = serviceLocator.GetInstance<IItemController>();
            this._itemSerializer = serviceLocator.GetInstance<IItemSerializer>();
            this._jsonSerializer = serviceLocator.GetInstance<IJsonSerializer>();
            this._clipboard = serviceLocator.GetInstance<IClipboard>();
            this._base64 = serviceLocator.GetInstance<IBase64>();
            this._pointController = serviceLocator.GetInstance<IPointController>();
            this._pageFactory = serviceLocator.GetInstance<IPageFactory>();

            this._lineMode = new SheetLineMode(this, serviceLocator);
            this._rectangleMode = new SheetRectangleMode(this, serviceLocator);
            this._ellipseMode = new SheetEllipseMode(this, serviceLocator);
        }

        #endregion

        #region Properties

        public IHistoryController HistoryController { get; set; }
        public ILibraryController LibraryController { get; set; }
        public IZoomController ZoomController { get; set; }
        public ICursorController CursorController { get; set; }
        public SheetOptions Options { get; set; }
        public ISheet EditorSheet { get; set; }
        public ISheet BackSheet { get; set; }
        public ISheet ContentSheet { get; set; }
        public ISheet OverlaySheet { get; set; }
        public ISheetView View { get; set; }
        public double LastFinalWidth { get; set; }
        public double LastFinalHeight { get; set; }

        #endregion

        #region Fields

        private SheetMode Mode = SheetMode.Selection;
        private SheetMode TempMode = SheetMode.None;
        private IBlock SelectedBlock;
        private IBlock ContentBlock;
        private IBlock FrameBlock;
        private IBlock GridBlock;
        private IRectangle TempSelectionRect;
        private bool IsFirstMove = true;
        private ImmutablePoint PanStartPoint;
        private ImmutablePoint SelectionStartPoint;
        private ItemType SelectedType = ItemType.None;
        private ILine SelectedLine;
        private IThumb LineThumbStart;
        private IThumb LineThumbEnd;
        private IElement SelectedElement;
        private IThumb ThumbTopLeft;
        private IThumb ThumbTopRight;
        private IThumb ThumbBottomLeft;
        private IThumb ThumbBottomRight;
        private ISelectedBlockPlugin InvertLineStartPlugin;
        private ISelectedBlockPlugin InvertLineEndPlugin;

        #endregion

        #region ToSingle

        public static IEnumerable<T> ToSingle<T>(T item)
        {
            yield return item;
        }

        #endregion

        #region Init

        public void Init()
        {
            SetDefaults();

            CreateBlocks();
            CreatePlugins();
            CreatePage();

            LoadLibraryFromResource(string.Concat("Sheet.Libraries", '.', "Digital.library"));
        }

        private void SetDefaults()
        {
            Options = new SheetOptions()
            {
                PageOriginX = 0.0,
                PageOriginY = 0.0,
                PageWidth = 1260.0,
                PageHeight = 891.0,
                SnapSize = 15,
                GridSize = 30,
                FrameThickness = 1.0,
                GridThickness = 1.0,
                SelectionThickness = 1.0,
                LineThickness = 2.0,
                HitTestSize = 3.5,
                DefaultZoomIndex = 9,
                MaxZoomIndex = 21,
                ZoomFactors = new double[] { 0.01, 0.0625, 0.0833, 0.125, 0.25, 0.3333, 0.5, 0.6667, 0.75, 1, 1.25, 1.5, 2, 3, 4, 6, 8, 12, 16, 24, 32, 64 }
            };

            ZoomController.ZoomIndex = Options.DefaultZoomIndex;
        }

        private void CreateBlocks()
        {
            ContentBlock = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "CONTENT", null);
            ContentBlock.Init();

            FrameBlock = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "FRAME", null);
            FrameBlock.Init();

            GridBlock = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "GRID", null);
            GridBlock.Init();

            SelectedBlock = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "SELECTED", null);
        }

        #endregion

        #region Blocks

        public IBlock GetSelected()
        {
            return SelectedBlock;
        }

        public IBlock GetContent()
        {
            return ContentBlock;
        }

        #endregion

        #region Page

        public async void SetPage(string text)
        {
            try
            {
                if (text == null)
                {
                    HistoryController.Reset();
                    ResetPage();
                }
                else
                {
                    var block = await Task.Run(() => _itemSerializer.DeserializeContents(text));
                    HistoryController.Reset();
                    ResetPage();
                    DeserializePage(block);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public string GetPage()
        {
            var block = SerializePage();
            var text = _itemSerializer.SerializeContents(block);

            return text;
        }

        public void ExportPage(string text)
        {
            var block = _itemSerializer.DeserializeContents(text);
            Export(ToSingle(block));
        }

        public void ExportPages(IEnumerable<string> texts)
        {
            var blocks = texts.Select(text => _itemSerializer.DeserializeContents(text));
            Export(blocks);
        }

        public BlockItem SerializePage()
        {
            _blockController.DeselectContent(SelectedBlock);

            var grid = _blockSerializer.SerializerContents(GridBlock, -1, GridBlock.X, GridBlock.Y, GridBlock.Width, GridBlock.Height, -1, "GRID");
            var frame = _blockSerializer.SerializerContents(FrameBlock, -1, FrameBlock.X, FrameBlock.Y, FrameBlock.Width, FrameBlock.Height, -1, "FRAME");
            var content = _blockSerializer.SerializerContents(ContentBlock, -1, ContentBlock.X, ContentBlock.Y, ContentBlock.Width, ContentBlock.Height, -1, "CONTENT");

            var page = new BlockItem();
            page.Init(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "PAGE");

            page.Blocks.Add(grid);
            page.Blocks.Add(frame);
            page.Blocks.Add(content);

            return page;
        }

        public void DeserializePage(BlockItem page)
        {
            BlockItem grid = page.Blocks.Where(block => block.Name == "GRID").FirstOrDefault();
            BlockItem frame = page.Blocks.Where(block => block.Name == "FRAME").FirstOrDefault();
            BlockItem content = page.Blocks.Where(block => block.Name == "CONTENT").FirstOrDefault();

            if (grid != null)
            {
                _blockController.AddContents(BackSheet, grid, GridBlock, null, false, Options.GridThickness / ZoomController.Zoom);
            }

            if (frame != null)
            {
                _blockController.AddContents(BackSheet, frame, FrameBlock, null, false, Options.FrameThickness / ZoomController.Zoom);
            }

            if (content != null)
            {
                _blockController.AddContents(ContentSheet, content, ContentBlock, null, false, Options.LineThickness / ZoomController.Zoom);
            }
        }

        public void ResetPage()
        {
            ResetOverlay();

            _blockController.Remove(BackSheet, GridBlock);
            _blockController.Remove(BackSheet, FrameBlock);
            _blockController.Remove(ContentSheet, ContentBlock);

            CreateBlocks();
        }

        public void ResetPageContent()
        {
            ResetOverlay();

            _blockController.Remove(ContentSheet, ContentBlock);
        }

        #endregion

        #region Mode

        public SheetMode GetMode()
        {
            return Mode;
        }

        public void SetMode(SheetMode mode)
        {
            Mode = mode;
        }

        private void StoreTempMode()
        {
            TempMode = GetMode();
        }

        private void RestoreTempMode()
        {
            SetMode(TempMode);
        }

        #endregion

        #region Clipboard

        public void CutText()
        {
            try
            {
                if (_blockController.HaveSelected(SelectedBlock))
                {
                    var copy = _blockController.ShallowCopy(SelectedBlock);
                    HistoryController.Register("Cut");
                    CopyText(copy);
                    Delete(copy);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void CopyText(IBlock block)
        {
            try
            {
                var selected = _blockSerializer.SerializerContents(block, -1, 0.0, 0.0, 0.0, 0.0, -1, "SELECTED");
                var text = _itemSerializer.SerializeContents(selected);
                _clipboard.Set(text);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public void CopyText()
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                CopyText(SelectedBlock);
            }
        }

        public async void PasteText()
        {
            try
            {
                var text = _clipboard.Get();
                var block = await Task.Run(() => _itemSerializer.DeserializeContents(text));
                HistoryController.Register("Paste");
                InsertContent(block, true);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public void CutJson()
        {
            try
            {
                if (_blockController.HaveSelected(SelectedBlock))
                {
                    var copy = _blockController.ShallowCopy(SelectedBlock);
                    HistoryController.Register("Cut");
                    CopyJson(copy);
                    Delete(copy);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void CopyJson(IBlock block)
        {
            try
            {
                var selected = _blockSerializer.SerializerContents(block, -1, 0.0, 0.0, 0.0, 0.0, -1, "SELECTED");
                string json = _jsonSerializer.Serialize(selected);
                _clipboard.Set(json);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public void CopyJson()
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                CopyJson(SelectedBlock);
            }
        }

        public async void PasteJson()
        {
            try
            {
                var text = _clipboard.Get();
                var block = await Task.Run(() => _jsonSerializer.Deerialize<BlockItem>(text));
                HistoryController.Register("Paste");
                InsertContent(block, true);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        #endregion

        #region Overlay

        private void ResetOverlay()
        {
            _lineMode.Reset();
            _rectangleMode.Reset();
            _ellipseMode.Reset();

            if (TempSelectionRect != null)
            {
                OverlaySheet.Remove(TempSelectionRect);
                TempSelectionRect = null;
            }

            if (LineThumbStart != null)
            {
                OverlaySheet.Remove(LineThumbStart);
            }

            if (LineThumbEnd != null)
            {
                OverlaySheet.Remove(LineThumbEnd);
            }
        }

        #endregion

        #region Delete

        public void Delete(IBlock block)
        {
            FinishEdit();
            _blockController.RemoveSelected(ContentSheet, ContentBlock, block);
        }

        public void Delete()
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                var copy = _blockController.ShallowCopy(SelectedBlock);
                HistoryController.Register("Delete");
                Delete(copy);
            }
        }

        #endregion

        #region Select All

        public void SelecteAll()
        {
            _blockController.SelectContent(SelectedBlock, ContentBlock);
        }

        #endregion

        #region Toggle Fill

        public void ToggleFill()
        {
            switch(GetMode())
            {
                case SheetMode.Line:
                    _lineMode.ToggleFill();
                    break;
                case SheetMode.Rectangle:
                    _rectangleMode.ToggleFill();
                    break;
                case SheetMode.Ellipse:
                    _ellipseMode.ToggleFill();
                    break;
            }
        }

        #endregion

        #region Insert Mode

        private void InsertContent(BlockItem block, bool select)
        {
            _blockController.DeselectContent(SelectedBlock);
            _blockController.AddContents(ContentSheet, block, ContentBlock, SelectedBlock, select, Options.LineThickness / ZoomController.Zoom);
        }

        private BlockItem CreateBlock(string name, IBlock block)
        {
            try
            {
                var blockItem = _blockSerializer.Serialize(block);
                blockItem.Name = name;
                return blockItem;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            return null;
        }

        public void CreateBlock()
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                StoreTempMode();
                SetMode(SheetMode.TextEditor);

                var tc = CreateTextEditor(new ImmutablePoint((EditorSheet.Width / 2) - (330 / 2), EditorSheet.Height / 2));

                Action<string> ok = (name) =>
                {
                    var block = CreateBlock(name, SelectedBlock);
                    if (block != null)
                    {
                        AddToLibrary(block);
                    }
                    EditorSheet.Remove(tc);
                    View.Focus();
                    RestoreTempMode();
                };

                Action cancel = () =>
                {
                    EditorSheet.Remove(tc);
                    View.Focus();
                    RestoreTempMode();
                };

                tc.Set(ok, cancel, "Create Block", "Name:", "BLOCK0");
                EditorSheet.Add(tc);
            }
        }

        public async void BreakBlock()
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                var text = _itemSerializer.SerializeContents(_blockSerializer.SerializerContents(SelectedBlock, 0, 0.0, 0.0, 0.0, 0.0, -1, "SELECTED"));
                var block = await Task.Run(() => _itemSerializer.DeserializeContents(text));
                HistoryController.Register("Break Block");
                Delete();
                _blockController.AddBroken(ContentSheet, block, ContentBlock, SelectedBlock, true, Options.LineThickness / ZoomController.Zoom);
            }
        }

        #endregion

        #region Point Mode

        public IPoint InsertPoint(ImmutablePoint p, bool register, bool select)
        {
            double thickness = Options.LineThickness / ZoomController.Zoom;
            double x = _itemController.Snap(p.X, Options.SnapSize);
            double y = _itemController.Snap(p.Y, Options.SnapSize);

            var point = _blockFactory.CreatePoint(thickness, x, y, false);

            if (register)
            {
                _blockController.DeselectContent(SelectedBlock);
                HistoryController.Register("Insert Point");
            }

            ContentBlock.Points.Add(point);
            ContentSheet.Add(point);

            if (select)
            {
                SelectedBlock.Points = new List<IPoint>();
                SelectedBlock.Points.Add(point);

                _blockController.Select(point);
            }

            return point;
        }

        #endregion

        #region Move Mode

        private void Move(double x, double y)
        {
            if (_blockController.HaveSelected(SelectedBlock))
            {
                IBlock moveBlock = _blockController.ShallowCopy(SelectedBlock);
                FinishEdit();
                HistoryController.Register("Move");
                _blockController.Select(moveBlock);
                SelectedBlock = moveBlock;
                _blockController.MoveDelta(x, y, SelectedBlock);
            }
        }

        public void MoveUp()
        {
            Move(0.0, -Options.SnapSize);
        }

        public void MoveDown()
        {
            Move(0.0, Options.SnapSize);
        }

        public void MoveLeft()
        {
            Move(-Options.SnapSize, 0.0);
        }

        public void MoveRight()
        {
            Move(Options.SnapSize, 0.0);
        }

        private bool CanInitMove(ImmutablePoint p)
        {
            var temp = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "TEMP", null);
            _blockController.HitTestClick(ContentSheet, SelectedBlock, temp, p, Options.HitTestSize, false, true);
            if (_blockController.HaveSelected(temp))
            {
                return true;
            }
            return false;
        }

        private void InitMove(ImmutablePoint p)
        {
            IsFirstMove = true;
            StoreTempMode();
            SetMode(SheetMode.Move);
            double x = _itemController.Snap(p.X, Options.SnapSize);
            double y = _itemController.Snap(p.Y, Options.SnapSize);
            PanStartPoint = new ImmutablePoint(x, y);
            ResetOverlay();
            OverlaySheet.Capture();
        }

        private void Move(ImmutablePoint p)
        {
            if (IsFirstMove)
            {
                IBlock moveBlock = _blockController.ShallowCopy(SelectedBlock);
                HistoryController.Register("Move");
                IsFirstMove = false;
                CursorController.Set(SheetCursor.Move);
                _blockController.Select(moveBlock);
                SelectedBlock = moveBlock;
            }

            double x = _itemController.Snap(p.X, Options.SnapSize);
            double y = _itemController.Snap(p.Y, Options.SnapSize);
            double dx = x - PanStartPoint.X;
            double dy = y - PanStartPoint.Y;

            if (dx != 0.0 || dy != 0.0)
            {
                _blockController.MoveDelta(dx, dy, SelectedBlock);
                PanStartPoint = new ImmutablePoint(x, y);
            }
        }

        private void FinishMove()
        {
            RestoreTempMode();
            CursorController.Set(SheetCursor.Normal);
            OverlaySheet.ReleaseCapture();
        }

        #endregion

        #region Pan & Zoom Mode

        public void SetAutoFitSize(double finalWidth, double finalHeight)
        {
            LastFinalWidth = finalWidth;
            LastFinalHeight = finalHeight;
        }

        private void ZoomTo(double x, double y, int oldZoomIndex)
        {
            double oldZoom = GetZoom(oldZoomIndex);
            double newZoom = GetZoom(ZoomController.ZoomIndex);
            ZoomController.Zoom = newZoom;

            ZoomController.PanX = (x * oldZoom + ZoomController.PanX) - x * newZoom;
            ZoomController.PanY = (y * oldZoom + ZoomController.PanY) - y * newZoom;
        }

        private void ZoomTo(int delta, ImmutablePoint p)
        {
            if (delta > 0)
            {

                if (ZoomController.ZoomIndex > -1 && ZoomController.ZoomIndex < Options.MaxZoomIndex)
                {
                    ZoomTo(p.X, p.Y, ZoomController.ZoomIndex++);
                }
            }
            else
            {
                if (ZoomController.ZoomIndex > 0)
                {
                    ZoomTo(p.X, p.Y, ZoomController.ZoomIndex--);
                }
            }
        }

        private double GetZoom(int index)
        {
            if (index >= 0 && index <= Options.MaxZoomIndex)
            {
                return Options.ZoomFactors[index];
            }
            return ZoomController.Zoom;
        }

        private void InitPan(ImmutablePoint p)
        {
            StoreTempMode();
            SetMode(SheetMode.Pan);
            PanStartPoint = new ImmutablePoint(p.X, p.Y);
            ResetOverlay();
            CursorController.Set(SheetCursor.Pan);
            OverlaySheet.Capture();
        }

        private void Pan(ImmutablePoint p)
        {
            ZoomController.PanX = ZoomController.PanX + p.X - PanStartPoint.X;
            ZoomController.PanY = ZoomController.PanY + p.Y - PanStartPoint.Y;
            PanStartPoint = new ImmutablePoint(p.X, p.Y);
        }

        private void FinishPan()
        {
            RestoreTempMode();
            CursorController.Set(SheetCursor.Normal);
            OverlaySheet.ReleaseCapture();
        }

        private void AdjustThickness(IEnumerable<ILine> lines, double thickness)
        {
            foreach (var line in lines)
            {
                _blockHelper.SetStrokeThickness(line, thickness);
            }
        }

        private void AdjustThickness(IEnumerable<IRectangle> rectangles, double thickness)
        {
            foreach (var rectangle in rectangles)
            {
                _blockHelper.SetStrokeThickness(rectangle, thickness);
            }
        }

        private void AdjustThickness(IEnumerable<IEllipse> ellipses, double thickness)
        {
            foreach (var ellipse in ellipses)
            {
                _blockHelper.SetStrokeThickness(ellipse, thickness);
            }
        }

        private void AdjustThickness(IBlock parent, double thickness)
        {
            if (parent != null)
            {
                AdjustThickness(parent.Lines, thickness);
                AdjustThickness(parent.Rectangles, thickness);
                AdjustThickness(parent.Ellipses, thickness);

                foreach (var block in parent.Blocks)
                {
                    AdjustThickness(block, thickness);
                }
            }
        }

        public void AdjustBackThickness(double zoom)
        {
            double gridThicknessZoomed = Options.GridThickness / zoom;
            double frameThicknessZoomed = Options.FrameThickness / zoom;

            AdjustThickness(GridBlock, gridThicknessZoomed);
            AdjustThickness(FrameBlock, frameThicknessZoomed);
        }

        public void AdjustPageThickness(double zoom)
        {
            double lineThicknessZoomed = Options.LineThickness / zoom;
            double selectionThicknessZoomed = Options.SelectionThickness / zoom;

            AdjustBackThickness(zoom);
            AdjustThickness(ContentBlock, lineThicknessZoomed);

            _lineMode.Adjust(zoom);
            _rectangleMode.Adjust(zoom);
            _ellipseMode.Adjust(zoom);

            if (TempSelectionRect != null)
            {
                _blockHelper.SetStrokeThickness(TempSelectionRect, selectionThicknessZoomed);
            }
        }

        #endregion

        #region Selection Mode

        private void InitSelectionRect(ImmutablePoint p)
        {
            SelectionStartPoint = new ImmutablePoint(p.X, p.Y);
            TempSelectionRect = _pageFactory.CreateSelectionRectangle(Options.SelectionThickness / ZoomController.Zoom, p.X, p.Y, 0.0, 0.0);
            OverlaySheet.Add(TempSelectionRect);
            OverlaySheet.Capture();
        }

        private void MoveSelectionRect(ImmutablePoint p)
        {
            double sx = SelectionStartPoint.X;
            double sy = SelectionStartPoint.Y;
            double x = p.X;
            double y = p.Y;
            double width = Math.Abs(sx - x);
            double height = Math.Abs(sy - y);
            _blockHelper.SetLeft(TempSelectionRect, Math.Min(sx, x));
            _blockHelper.SetTop(TempSelectionRect, Math.Min(sy, y));
            _blockHelper.SetWidth(TempSelectionRect, width);
            _blockHelper.SetHeight(TempSelectionRect, height);
        }

        private void FinishSelectionRect()
        {
            double x = _blockHelper.GetLeft(TempSelectionRect);
            double y = _blockHelper.GetTop(TempSelectionRect);
            double width = _blockHelper.GetWidth(TempSelectionRect);
            double height = _blockHelper.GetHeight(TempSelectionRect);

            CancelSelectionRect();

            // get selected items
            bool onlyCtrl = Keyboard.Modifiers == ModifierKeys.Control;
            bool resetSelected = onlyCtrl && _blockController.HaveSelected(SelectedBlock) ? false : true;
            _blockController.HitTestSelectionRect(ContentSheet, ContentBlock, SelectedBlock, new ImmutableRect(x, y, width, height), resetSelected);

            // edit mode
            TryToEditSelected();
        }

        private void CancelSelectionRect()
        {
            OverlaySheet.ReleaseCapture();
            OverlaySheet.Remove(TempSelectionRect);
            TempSelectionRect = null;
        }

        #endregion

        #region Text Mode

        private TextControl CreateTextEditor(ImmutablePoint p)
        {
            var tc = new TextControl() { Width = 330.0, Background = Brushes.WhiteSmoke };
            tc.RenderTransform = null;
            Canvas.SetLeft(tc, p.X);
            Canvas.SetTop(tc, p.Y);
            return tc;
        }

        private void CreateText(ImmutablePoint p)
        {
            double x = _itemController.Snap(p.X, Options.SnapSize);
            double y = _itemController.Snap(p.Y, Options.SnapSize);
            HistoryController.Register("Create Text");

            var text = _blockFactory.CreateText("Text", x, y, 30.0, 15.0, (int)XHorizontalAlignment.Center, (int)XVerticalAlignment.Center, 11.0, ItemColors.Transparent, ItemColors.Black);
            ContentBlock.Texts.Add(text);
            ContentSheet.Add(text);
        }

        private bool TryToEditText(ImmutablePoint p)
        {
            var temp = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "TEMP", null);
            _blockController.HitTestClick(ContentSheet, ContentBlock, temp, p, Options.HitTestSize, true, true);

            if (_blockController.HaveOneTextSelected(temp))
            {
                var tb = WpfBlockHelper.GetTextBlock(temp.Texts[0]);

                StoreTempMode();
                SetMode(SheetMode.TextEditor);

                var tc = CreateTextEditor(new ImmutablePoint((EditorSheet.Width / 2) - (330 / 2), EditorSheet.Height / 2) /* p */);

                Action<string> ok = (text) =>
                {
                    HistoryController.Register("Edit Text");
                    tb.Text = text;
                    EditorSheet.Remove(tc);
                    View.Focus();
                    RestoreTempMode();
                };

                Action cancel = () =>
                {
                    EditorSheet.Remove(tc);
                    View.Focus();
                    RestoreTempMode();
                };

                tc.Set(ok, cancel, "Edit Text", "Text:", tb.Text);
                EditorSheet.Add(tc);

                _blockController.Deselect(temp);
                return true;
            }

            _blockController.Deselect(temp);
            return false;
        }

        #endregion

        #region Image Mode

        private void Image(ImmutablePoint p)
        {
            var dlg = _serviceLocator.GetInstance<IOpenFileDialog>();
            dlg.Filter = FileDialogSettings.ImageFilter;
            dlg.FilterIndex = 1;
            dlg.FileName = "";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    InsertImage(p, dlg.FileName);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    Debug.Print(ex.StackTrace);
                }
            }
        }

        private void InsertImage(ImmutablePoint p, string path)
        {
            byte[] data = _base64.ReadAllBytes(path);
            double x = _itemController.Snap(p.X, Options.SnapSize);
            double y = _itemController.Snap(p.Y, Options.SnapSize);
            var image = _blockFactory.CreateImage(x, y, 120.0, 90.0, data);
            ContentBlock.Images.Add(image);
            ContentSheet.Add(image);
        }

        #endregion

        #region Edit Mode

        private bool TryToEditSelected()
        {
            if (_blockController.HaveOneLineSelected(SelectedBlock))
            {
                InitLineEditor();
                return true;
            }
            else if (_blockController.HaveOneRectangleSelected(SelectedBlock))
            {
                InitRectangleEditor();
                return true;
            }
            else if (_blockController.HaveOneEllipseSelected(SelectedBlock))
            {
                InitEllipseEditor();
                return true;
            }
            else if (_blockController.HaveOneTextSelected(SelectedBlock))
            {
                InitTextEditor();
                return true;
            }
            else if (_blockController.HaveOneImageSelected(SelectedBlock))
            {
                InitImageEditor();
                return true;
            }
            return false;
        }

        private void FinishEdit()
        {
            switch (SelectedType)
            {
                case ItemType.Line:
                    FinishLineEditor();
                    break;
                case ItemType.Rectangle:
                case ItemType.Ellipse:
                case ItemType.Text:
                case ItemType.Image:
                    FinishFrameworkElementEditor();
                    break;
            }
        }

        #endregion

        #region Edit Line

        private void DragLineStart(ILine line, IThumb thumb, double dx, double dy)
        {
            if (line != null && thumb != null)
            {
                if (line.Start != null)
                {
                    double x = _itemController.Snap(line.Start.X + dx, Options.SnapSize);
                    double y = _itemController.Snap(line.Start.Y + dy, Options.SnapSize);
                    double sdx = x - line.Start.X;
                    double sdy = y - line.Start.Y;
                    _blockController.MoveDelta(sdx, sdy, line.Start);
                    _blockHelper.SetLeft(thumb, x);
                    _blockHelper.SetTop(thumb, y);
                }
                else
                {
                    double x = _itemController.Snap(_blockHelper.GetX1(line) + dx, Options.SnapSize);
                    double y = _itemController.Snap(_blockHelper.GetY1(line) + dy, Options.SnapSize);
                    _blockHelper.SetX1(line, x);
                    _blockHelper.SetY1(line, y);
                    _blockHelper.SetLeft(thumb, x);
                    _blockHelper.SetTop(thumb, y);
                }
            }
        }

        private void DragLineEnd(ILine line, IThumb thumb, double dx, double dy)
        {
            if (line != null && thumb != null)
            {
                if (line.End != null)
                {
                    double x = _itemController.Snap(line.End.X + dx, Options.SnapSize);
                    double y = _itemController.Snap(line.End.Y + dy, Options.SnapSize);
                    double sdx = x - line.End.X;
                    double sdy = y - line.End.Y;
                    _blockController.MoveDelta(sdx, sdy, line.End);
                    _blockHelper.SetLeft(thumb, x);
                    _blockHelper.SetTop(thumb, y);
                }
                else
                {
                    double x = _itemController.Snap(_blockHelper.GetX2(line) + dx, Options.SnapSize);
                    double y = _itemController.Snap(_blockHelper.GetY2(line) + dy, Options.SnapSize);
                    _blockHelper.SetX2(line, x);
                    _blockHelper.SetY2(line, y);
                    _blockHelper.SetLeft(thumb, x);
                    _blockHelper.SetTop(thumb, y);
                }
            }
        }

        private void InitLineEditor()
        {
            StoreTempMode();
            SetMode(SheetMode.Edit);

            try
            {
                SelectedType = ItemType.Line;
                SelectedLine = SelectedBlock.Lines.FirstOrDefault();

                LineThumbStart = _blockFactory.CreateThumb(0.0, 0.0, SelectedLine, DragLineStart);
                LineThumbEnd = _blockFactory.CreateThumb(0.0, 0.0, SelectedLine, DragLineEnd);

                _blockHelper.SetLeft(LineThumbStart, _blockHelper.GetX1(SelectedLine));
                _blockHelper.SetTop(LineThumbStart, _blockHelper.GetY1(SelectedLine));
                _blockHelper.SetLeft(LineThumbEnd, _blockHelper.GetX2(SelectedLine));
                _blockHelper.SetTop(LineThumbEnd, _blockHelper.GetY2(SelectedLine));

                OverlaySheet.Add(LineThumbStart);
                OverlaySheet.Add(LineThumbEnd);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void FinishLineEditor()
        {
            RestoreTempMode();

            SelectedType = ItemType.None;
            SelectedLine = null;

            if (LineThumbStart != null)
            {
                OverlaySheet.Remove(LineThumbStart);
                LineThumbStart = null;
            }

            if (LineThumbEnd != null)
            {
                OverlaySheet.Remove(LineThumbEnd);
                LineThumbEnd = null;
            }
        }

        #endregion

        #region Edit FrameworkElement

        private void DragThumbs(Rect rect)
        {
            var tl = rect.TopLeft;
            var tr = rect.TopRight;
            var bl = rect.BottomLeft;
            var br = rect.BottomRight;

            _blockHelper.SetLeft(ThumbTopLeft, tl.X);
            _blockHelper.SetTop(ThumbTopLeft, tl.Y);
            _blockHelper.SetLeft(ThumbTopRight, tr.X);
            _blockHelper.SetTop(ThumbTopRight, tr.Y);
            _blockHelper.SetLeft(ThumbBottomLeft, bl.X);
            _blockHelper.SetTop(ThumbBottomLeft, bl.Y);
            _blockHelper.SetLeft(ThumbBottomRight, br.X);
            _blockHelper.SetTop(ThumbBottomRight, br.Y);
        }

        private void DragTopLeft(IElement element, IThumb thumb, double dx, double dy)
        {
            if (element != null && thumb != null)
            {
                double left = _blockHelper.GetLeft(element);
                double top = _blockHelper.GetTop(element);
                double width = _blockHelper.GetWidth(element);
                double height = _blockHelper.GetHeight(element);

                var rect = new Rect(left, top, width, height);
                rect.X = _itemController.Snap(rect.X + dx, Options.SnapSize);
                rect.Y = _itemController.Snap(rect.Y + dy, Options.SnapSize);
                rect.Width = Math.Max(0.0, rect.Width - (rect.X - left));
                rect.Height = Math.Max(0.0, rect.Height - (rect.Y - top));

                _blockHelper.SetLeft(element, rect.X);
                _blockHelper.SetTop(element, rect.Y);
                _blockHelper.SetWidth(element, rect.Width);
                _blockHelper.SetHeight(element, rect.Height);

                DragThumbs(rect);
            }
        }

        private void DragTopRight(IElement element, IThumb thumb, double dx, double dy)
        {
            if (element != null && thumb != null)
            {
                double left = _blockHelper.GetLeft(element);
                double top = _blockHelper.GetTop(element);
                double width = _blockHelper.GetWidth(element);
                double height = _blockHelper.GetHeight(element);

                var rect = new Rect(left, top, width, height);
                rect.Width = Math.Max(0.0, _itemController.Snap(rect.Width + dx, Options.SnapSize));
                rect.Y = _itemController.Snap(rect.Y + dy, Options.SnapSize);
                rect.Height = Math.Max(0.0, rect.Height - (rect.Y - top));

                _blockHelper.SetLeft(element, rect.X);
                _blockHelper.SetTop(element, rect.Y);
                _blockHelper.SetWidth(element, rect.Width);
                _blockHelper.SetHeight(element, rect.Height);

                DragThumbs(rect);
            }
        }

        private void DragBottomLeft(IElement element, IThumb thumb, double dx, double dy)
        {
            if (element != null && thumb != null)
            {
                double left = _blockHelper.GetLeft(element);
                double top = _blockHelper.GetTop(element);
                double width = _blockHelper.GetWidth(element);
                double height = _blockHelper.GetHeight(element);

                var rect = new Rect(left, top, width, height);
                rect.X = _itemController.Snap(rect.X + dx, Options.SnapSize);
                rect.Height = Math.Max(0.0, _itemController.Snap(rect.Height + dy, Options.SnapSize));
                rect.Width = Math.Max(0.0, rect.Width - (rect.X - left));

                _blockHelper.SetLeft(element, rect.X);
                _blockHelper.SetTop(element, rect.Y);
                _blockHelper.SetWidth(element, rect.Width);
                _blockHelper.SetHeight(element, rect.Height);

                DragThumbs(rect);
            }
        }

        private void DragBottomRight(IElement element, IThumb thumb, double dx, double dy)
        {
            if (element != null && thumb != null)
            {
                double left = _blockHelper.GetLeft(element);
                double top = _blockHelper.GetTop(element);
                double width = _blockHelper.GetWidth(element);
                double height = _blockHelper.GetHeight(element);

                var rect = new Rect(left, top, width, height);
                rect.Width = Math.Max(0.0, _itemController.Snap(rect.Width + dx, Options.SnapSize));
                rect.Height = Math.Max(0.0, _itemController.Snap(rect.Height + dy, Options.SnapSize));

                _blockHelper.SetLeft(element, rect.X);
                _blockHelper.SetTop(element, rect.Y);
                _blockHelper.SetWidth(element, rect.Width);
                _blockHelper.SetHeight(element, rect.Height);

                DragThumbs(rect);
            }
        }

        private void InitFrameworkElementEditor()
        {
            double left = _blockHelper.GetLeft(SelectedElement);
            double top = _blockHelper.GetTop(SelectedElement);
            double width = _blockHelper.GetWidth(SelectedElement);
            double height = _blockHelper.GetHeight(SelectedElement);

            ThumbTopLeft = _blockFactory.CreateThumb(0.0, 0.0, SelectedElement, DragTopLeft);
            ThumbTopRight = _blockFactory.CreateThumb(0.0, 0.0, SelectedElement, DragTopRight);
            ThumbBottomLeft = _blockFactory.CreateThumb(0.0, 0.0, SelectedElement, DragBottomLeft);
            ThumbBottomRight = _blockFactory.CreateThumb(0.0, 0.0, SelectedElement, DragBottomRight);

            _blockHelper.SetLeft(ThumbTopLeft, left);
            _blockHelper.SetTop(ThumbTopLeft, top);
            _blockHelper.SetLeft(ThumbTopRight, left + width);
            _blockHelper.SetTop(ThumbTopRight, top);
            _blockHelper.SetLeft(ThumbBottomLeft, left);
            _blockHelper.SetTop(ThumbBottomLeft, top + height);
            _blockHelper.SetLeft(ThumbBottomRight, left + width);
            _blockHelper.SetTop(ThumbBottomRight, top + height);

            OverlaySheet.Add(ThumbTopLeft);
            OverlaySheet.Add(ThumbTopRight);
            OverlaySheet.Add(ThumbBottomLeft);
            OverlaySheet.Add(ThumbBottomRight);
        }

        private void FinishFrameworkElementEditor()
        {
            RestoreTempMode();

            SelectedType = ItemType.None;
            SelectedElement = null;

            if (ThumbTopLeft != null)
            {
                OverlaySheet.Remove(ThumbTopLeft);
                ThumbTopLeft = null;
            }

            if (ThumbTopRight != null)
            {
                OverlaySheet.Remove(ThumbTopRight);
                ThumbTopRight = null;
            }

            if (ThumbBottomLeft != null)
            {
                OverlaySheet.Remove(ThumbBottomLeft);
                ThumbBottomLeft = null;
            }

            if (ThumbBottomRight != null)
            {
                OverlaySheet.Remove(ThumbBottomRight);
                ThumbBottomRight = null;
            }
        }

        #endregion

        #region Edit Rectangle

        private void InitRectangleEditor()
        {
            StoreTempMode();
            SetMode(SheetMode.Edit);

            try
            {
                var rectangle = SelectedBlock.Rectangles.FirstOrDefault();
                SelectedType = ItemType.Rectangle;
                SelectedElement = rectangle;
                InitFrameworkElementEditor();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        #endregion

        #region Edit Ellipse

        private void InitEllipseEditor()
        {
            StoreTempMode();
            SetMode(SheetMode.Edit);

            try
            {
                var ellipse = SelectedBlock.Ellipses.FirstOrDefault();
                SelectedType = ItemType.Ellipse;
                SelectedElement = ellipse;
                InitFrameworkElementEditor();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        #endregion

        #region Edit Text

        private void InitTextEditor()
        {
            StoreTempMode();
            SetMode(SheetMode.Edit);

            try
            {
                var text = SelectedBlock.Texts.FirstOrDefault();
                SelectedType = ItemType.Text;
                SelectedElement = text;
                InitFrameworkElementEditor();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        #endregion

        #region Edit Image

        private void InitImageEditor()
        {
            StoreTempMode();
            SetMode(SheetMode.Edit);

            try
            {
                var image = SelectedBlock.Images.FirstOrDefault();
                SelectedType = ItemType.Image;
                SelectedElement = image;
                InitFrameworkElementEditor();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        #endregion

        #region Data Binding

        public bool BindDataToBlock(ImmutablePoint p, DataItem dataItem)
        {
            var temp = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "TEMP", null);
            _blockController.HitTestForBlocks(ContentSheet, ContentBlock, temp, p, Options.HitTestSize);

            if (_blockController.HaveOneBlockSelected(temp))
            {
                HistoryController.Register("Bind Data");
                var block = temp.Blocks[0];
                var result = BindDataToBlock(block, dataItem);
                _blockController.Deselect(temp);

                if (result == true)
                {
                    _blockController.Select(block);
                    SelectedBlock.Blocks = new List<IBlock>();
                    SelectedBlock.Blocks.Add(block);
                }

                return true;
            }

            _blockController.Deselect(temp);
            return false;
        }

        public bool BindDataToBlock(IBlock block, DataItem dataItem)
        {
            if (block != null && block.Texts != null
                && dataItem != null && dataItem.Columns != null && dataItem.Data != null
                && block.Texts.Count == dataItem.Columns.Length - 1)
            {
                // assign block data id
                block.DataId = int.Parse(dataItem.Data[0]);

                // skip index column
                int i = 1;

                foreach (var text in block.Texts)
                {
                    var tb = WpfBlockHelper.GetTextBlock(text);
                    tb.Text = dataItem.Data[i];
                    i++;
                }

                return true;
            }

            return false;
        }

        public void TryToBindData(ImmutablePoint p, DataItem dataItem)
        {
            // first try binding to existing block
            bool firstTryResult = BindDataToBlock(p, dataItem);

            // if failed insert selected block from library and try again to bind
            if (!firstTryResult)
            {
                var blockItem = LibraryController.GetSelected();
                if (blockItem != null)
                {
                    var block = Insert(blockItem, p, false);
                    bool secondTryResult = BindDataToBlock(block, dataItem);
                    if (!secondTryResult)
                    {
                        // remove block if failed to bind
                        var temp = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "TEMP", null);
                        temp.Init();
                        temp.Blocks.Add(block);
                        _blockController.RemoveSelected(ContentSheet, ContentBlock, temp);
                    }
                    else
                    {
                        _blockController.Select(block);
                        SelectedBlock.Blocks = new List<IBlock>();
                        SelectedBlock.Blocks.Add(block);
                    }
                }
            }
        }

        #endregion

        #region New Page

        public void NewPage()
        {
            HistoryController.Register("New");
            ResetPage();
            CreatePage();
            ZoomController.AutoFit();
        }

        #endregion

        #region Open Page

        public async Task OpenTextPage(string path)
        {
            var text = await _itemController.OpenText(path);
            if (text != null)
            {
                var page = await Task.Run(() => _itemSerializer.DeserializeContents(text));
                HistoryController.Register("Open Text");
                ResetPage();
                DeserializePage(page);
            }
        }

        public async Task OpenJsonPage(string path)
        {
            var text = await _itemController.OpenText(path);
            if (text != null)
            {
                var page = await Task.Run(() => _jsonSerializer.Deerialize<BlockItem>(text));
                HistoryController.Register("Open Json");
                ResetPage();
                DeserializePage(page);
            }
        }

        public async void OpenPage()
        {
            var dlg = _serviceLocator.GetInstance<IOpenFileDialog>();
            dlg.Filter = FileDialogSettings.PageFilter;
            dlg.FilterIndex = 1;
            dlg.FileName = "";

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                switch (dlg.FilterIndex)
                {
                    case 1:
                        {
                            try
                            {
                                await OpenTextPage(path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                    case 2:
                    case 3:
                        {
                            try
                            {
                                await OpenJsonPage(path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                }
            }
        }

        #endregion

        #region Save Page

        public void SaveTextPage(string path)
        {
            var page = SerializePage();

            Task.Run(() =>
            {
                var text = _itemSerializer.SerializeContents(page);
                _itemController.SaveText(path, text);
            });
        }

        public void SaveJsonPage(string path)
        {
            var page = SerializePage();

            Task.Run(() =>
            {
                string text = _jsonSerializer.Serialize(page);
                _itemController.SaveText(path, text);
            });
        }

        public void SavePage()
        {
            var dlg = _serviceLocator.GetInstance<ISaveFileDialog>();
            dlg.Filter = FileDialogSettings.PageFilter;
            dlg.FilterIndex = 1;
            dlg.FileName = "sheet";

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                switch (dlg.FilterIndex)
                {
                    case 1:
                        {
                            try
                            {
                                SaveTextPage(path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                    case 2:
                    case 3:
                        {
                            try
                            {
                                SaveJsonPage(path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                }
            }
        }

        #endregion

        #region Export Page

        private void ExportToPdf(IEnumerable<BlockItem> blocks, string fileName)
        {
            var pages = blocks.Select(content => CreatePage(content, true, false)).ToList();

            Task.Run(() =>
            {
                var writer = new BlockPdfWriter();
                writer.Create(fileName, Options.PageWidth, Options.PageHeight, pages);
                Process.Start(fileName);
            });
        }

        private void ExportToDxf(IEnumerable<BlockItem> blocks, string fileName)
        {
            var pages = blocks.Select(block => CreatePage(block, true, false)).ToList();

            Task.Run(() =>
            {
                var writer = new BlockDxfWriter();

                if (blocks.Count() > 1)
                {
                    string path = System.IO.Path.GetDirectoryName(fileName);
                    string name = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string extension = System.IO.Path.GetExtension(fileName);

                    int counter = 0;
                    foreach (var page in pages)
                    {
                        string fileNameWithCounter = System.IO.Path.Combine(path, string.Concat(name, '-', counter.ToString("000"), extension));
                        writer.Create(fileNameWithCounter, Options.PageWidth, Options.PageHeight, page);
                        counter++;
                    }
                }
                else
                {
                    var page = pages.FirstOrDefault();
                    if (page != null)
                    {
                        writer.Create(fileName, Options.PageWidth, Options.PageHeight, page);
                    }
                }
            });
        }

        public void Export(IEnumerable<BlockItem> blocks)
        {
            var dlg = _serviceLocator.GetInstance<ISaveFileDialog>();
            dlg.Filter = FileDialogSettings.ExportFilter;
            dlg.FilterIndex = 1;
            dlg.FileName = "sheet";

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                switch (dlg.FilterIndex)
                {
                    case 1:
                    case 3:
                    default:
                        {
                            try
                            {
                                ExportToPdf(blocks, path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                    case 2:
                        {
                            try
                            {
                                ExportToDxf(blocks, path);
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                                Debug.Print(ex.StackTrace);
                            }
                        }
                        break;
                }
            }
        }

        public void Export(SolutionEntry solution)
        {
            var texts = solution.Documents.SelectMany(document => document.Pages).Select(page => page.Content);
            ExportPages(texts);
        }

        public void ExportPage()
        {
            var block = _blockSerializer.SerializerContents(ContentBlock, -1, ContentBlock.X, ContentBlock.Y, ContentBlock.Width, ContentBlock.Height, ContentBlock.DataId, "CONTENT");
            var blocks = ToSingle(block);
            Export(blocks);
        }

        #endregion

        #region Library

        public void Insert(ImmutablePoint p)
        {
            if (LibraryController != null)
            {
                var blockItem = LibraryController.GetSelected() as BlockItem;
                Insert(blockItem, p, true);
            }
        }

        public IBlock Insert(BlockItem blockItem, ImmutablePoint p, bool select)
        {
            _blockController.DeselectContent(SelectedBlock);
            double thickness = Options.LineThickness / ZoomController.Zoom;

            HistoryController.Register("Insert Block");

            var block = _blockSerializer.Deserialize(ContentSheet, ContentBlock, blockItem, thickness);

            if (select)
            {
                _blockController.Select(block);
                SelectedBlock.Blocks = new List<IBlock>();
                SelectedBlock.Blocks.Add(block);
            }

            _blockController.MoveDelta(_itemController.Snap(p.X, Options.SnapSize), _itemController.Snap(p.Y, Options.SnapSize), block);

            return block;
        }

        private async void LoadLibraryFromResource(string name)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                return;
            }

            using (var stream = assembly.GetManifestResourceStream(name))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string text = await reader.ReadToEndAsync();
                    if (text != null)
                    {
                        InitLibrary(text);
                    }
                }
            }
        }

        public async Task LoadLibrary(string path)
        {
            var text = await _itemController.OpenText(path);
            if (text != null)
            {
                InitLibrary(text);
            }
        }

        private async void InitLibrary(string text)
        {
            if (LibraryController != null && text != null)
            {
                var block = await Task.Run(() => _itemSerializer.DeserializeContents(text));
                LibraryController.SetSource(block.Blocks);
            }
        }

        private void AddToLibrary(BlockItem blockItem)
        {
            if (LibraryController != null && blockItem != null)
            {
                var source = LibraryController.GetSource();
                var items = new List<BlockItem>(source);
                _itemController.ResetPosition(blockItem, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight);
                items.Add(blockItem);
                LibraryController.SetSource(items);
            }
        }

        public async void LoadLibrary()
        {
            var dlg = _serviceLocator.GetInstance<IOpenFileDialog>();
            dlg.Filter = FileDialogSettings.LibraryFilter;
            dlg.FilterIndex = 1;
            dlg.FileName = "";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    await LoadLibrary(dlg.FileName);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    Debug.Print(ex.StackTrace);
                }
            }
        }

        #endregion

        #region Point

        public IPoint TryToFindPoint(ImmutablePoint p)
        {
            var temp = _blockFactory.CreateBlock(-1, Options.PageOriginX, Options.PageOriginY, Options.PageWidth, Options.PageHeight, -1, "TEMP", null);
            _blockController.HitTestClick(ContentSheet, GetContent(), temp, p, Options.HitTestSize, true, true);

            if (_blockController.HaveOnePointSelected(temp))
            {
                var xpoint = temp.Points[0];
                _blockController.Deselect(temp);
                return xpoint;
            }

            _blockController.Deselect(temp);
            return null;
        } 

        #endregion

        #region Input

        public void LeftDown(InputArgs args)
        {
            // edit mode
            if (SelectedType != ItemType.None)
            {
                if (args.SourceType != ItemType.Thumb)
                {
                    _blockController.DeselectContent(SelectedBlock);
                    FinishEdit();
                }
                else
                {
                    return;
                }
            }

            // text editor
            if (GetMode() == SheetMode.None || GetMode() == SheetMode.TextEditor)
            {
                return;
            }

            // move mode
            if (!args.OnlyControl)
            {
                if (_blockController.HaveSelected(SelectedBlock) && CanInitMove(args.SheetPosition))
                {
                    InitMove(args.SheetPosition);
                    return;
                }

                _blockController.DeselectContent(SelectedBlock);
            }

            bool resetSelected = args.OnlyControl && _blockController.HaveSelected(SelectedBlock) ? false : true;

            if (GetMode() == SheetMode.Selection)
            {
                bool result = _blockController.HitTestClick(ContentSheet, ContentBlock, SelectedBlock, new ImmutablePoint(args.SheetPosition.X, args.SheetPosition.Y), Options.HitTestSize, false, resetSelected);
                if ((args.OnlyControl || !_blockController.HaveSelected(SelectedBlock)) && !result)
                {
                    InitSelectionRect(args.SheetPosition);
                }
                else
                {
                    // TODO: If control key is pressed then switch to move mode instead to edit mode
                    bool editModeEnabled = args.OnlyControl == true ? false : TryToEditSelected();
                    if (!editModeEnabled)
                    {
                        InitMove(args.SheetPosition);
                    }
                }
            }
            else if (GetMode() == SheetMode.Insert && !OverlaySheet.IsCaptured)
            {
                Insert(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Point && !OverlaySheet.IsCaptured)
            {
                InsertPoint(args.SheetPosition, true, true);
            }
            else if (GetMode() == SheetMode.Line && !OverlaySheet.IsCaptured)
            {
                // try to find point to connect line start
                IPoint start = TryToFindPoint(args.SheetPosition);

                // create start if Control key is pressed and start point has not been found
                if (args.OnlyControl && start == null)
                {
                    start = InsertPoint(args.SheetPosition, true, false);
                }

                _lineMode.Init(args.SheetPosition, start);
            }
            else if (GetMode() == SheetMode.Line && OverlaySheet.IsCaptured)
            {
                // try to find point to connect line end
                IPoint end = TryToFindPoint(args.SheetPosition);

                // create end point if Control key is pressed and end point has not been found
                if (args.OnlyControl && end == null)
                {
                    end = InsertPoint(args.SheetPosition, true, false);
                }

                _lineMode.Finish(end);
            }
            else if (GetMode() == SheetMode.Rectangle && !OverlaySheet.IsCaptured)
            {
                _rectangleMode.Init(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Rectangle && OverlaySheet.IsCaptured)
            {
                _rectangleMode.Finish();
            }
            else if (GetMode() == SheetMode.Ellipse && !OverlaySheet.IsCaptured)
            {
                _ellipseMode.Init(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Ellipse && OverlaySheet.IsCaptured)
            {
                _ellipseMode.Finish();
            }
            else if (GetMode() == SheetMode.Pan && OverlaySheet.IsCaptured)
            {
                FinishPan();
            }
            else if (GetMode() == SheetMode.Text && !OverlaySheet.IsCaptured)
            {
                CreateText(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Image && !OverlaySheet.IsCaptured)
            {
                Image(args.SheetPosition);
            }
        }

        public void LeftUp(InputArgs args)
        {
            if (GetMode() == SheetMode.Selection && OverlaySheet.IsCaptured)
            {
                FinishSelectionRect();
            }
            else if (GetMode() == SheetMode.Move && OverlaySheet.IsCaptured)
            {
                FinishMove();
            }
        }

        public void Move(InputArgs args)
        {
            if (GetMode() == SheetMode.Edit)
            {
                return;
            }

            // mouse over selection when holding Shift key
            if (args.OnlyShift && TempSelectionRect == null && !OverlaySheet.IsCaptured)
            {
                if (_blockController.HaveSelected(SelectedBlock))
                {
                    _blockController.DeselectContent(SelectedBlock);
                }

                _blockController.HitTestClick(ContentSheet, ContentBlock, SelectedBlock, args.SheetPosition, Options.HitTestSize, false, false);
            }

            if (GetMode() == SheetMode.Selection && OverlaySheet.IsCaptured)
            {
                MoveSelectionRect(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Line && OverlaySheet.IsCaptured)
            {
                _lineMode.Move(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Rectangle && OverlaySheet.IsCaptured)
            {
                _rectangleMode.Move(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Ellipse && OverlaySheet.IsCaptured)
            {
                _ellipseMode.Move(args.SheetPosition);
            }
            else if (GetMode() == SheetMode.Pan && OverlaySheet.IsCaptured)
            {
                Pan(args.RootPosition);
            }
            else if (GetMode() == SheetMode.Move && OverlaySheet.IsCaptured)
            {
                Move(args.SheetPosition);
            }
        }

        public void RightDown(InputArgs args)
        {
            if (GetMode() == SheetMode.None || GetMode() == SheetMode.TextEditor)
            {
                return;
            }

            // edit mode
            if (SelectedType != ItemType.None)
            {
                _blockController.DeselectContent(SelectedBlock);
                FinishEdit();
                return;
            }

            // text editor
            if (GetMode() == SheetMode.Text && TryToEditText(args.SheetPosition))
            {
                args.Handled(true);
                return;
            }
            else
            {
                _blockController.DeselectContent(SelectedBlock);

                if (GetMode() == SheetMode.Selection && OverlaySheet.IsCaptured)
                {
                    CancelSelectionRect();
                }
                else if (GetMode() == SheetMode.Line && OverlaySheet.IsCaptured)
                {
                    _lineMode.Cancel();
                }
                else if (GetMode() == SheetMode.Rectangle && OverlaySheet.IsCaptured)
                {
                    _rectangleMode.Cancel();
                }
                else if (GetMode() == SheetMode.Ellipse && OverlaySheet.IsCaptured)
                {
                    _ellipseMode.Cancel();
                }
                else if (!OverlaySheet.IsCaptured)
                {
                    InitPan(args.RootPosition);
                }
            }
        }

        public void RightUp(InputArgs args)
        {
            if (GetMode() == SheetMode.Pan && OverlaySheet.IsCaptured)
            {
                FinishPan();
            }
        }

        public void Wheel(int delta, ImmutablePoint position)
        {
            ZoomTo(delta, position);
        }

        public void Down(InputArgs args)
        {
            if (args.Button == InputButton.Middle && args.Clicks == 2)
            {
                // Mouse Middle Double-Click + Control key pressed to reset Pan and Zoom
                // Mouse Middle Double-Click to Auto Fit page to window size
                if (args.OnlyControl)
                {
                    ZoomController.ActualSize();
                }
                else
                {
                    ZoomController.AutoFit();
                }
            }
        }

        #endregion

        #region Page Frame & Grid

        private void CreatePage()
        {
            _pageFactory.CreateGrid(BackSheet, GridBlock, 330.0, 30.0, 600.0, 750.0, Options.GridSize, Options.GridThickness, ItemColors.LightGray);
            _pageFactory.CreateFrame(BackSheet, FrameBlock, Options.GridSize, Options.GridThickness, ItemColors.DarkGray);

            AdjustThickness(GridBlock, Options.GridThickness / GetZoom(ZoomController.ZoomIndex));
            AdjustThickness(FrameBlock, Options.FrameThickness / GetZoom(ZoomController.ZoomIndex));
        }

        private BlockItem CreateGridBlock(IBlock gridBlock, bool adjustThickness, bool adjustColor)
        {
            var grid = _blockSerializer.SerializerContents(gridBlock, -1, 0.0, 0.0, 0.0, 0.0, -1, "GRID");

            // lines
            foreach (var lineItem in grid.Lines)
            {
                if (adjustThickness)
                {
                    lineItem.StrokeThickness = 0.013 * 72.0 / 2.54; // 0.13mm 
                }

                if (adjustColor)
                {
                    lineItem.Stroke = ItemColors.Black;
                }
            }

            return grid;
        }

        private BlockItem CreateFrameBlock(IBlock frameBlock, bool adjustThickness, bool adjustColor)
        {
            var frame = _blockSerializer.SerializerContents(frameBlock, -1, 0.0, 0.0, 0.0, 0.0, -1, "FRAME");

            // texts
            foreach (var textItem in frame.Texts)
            {
                if (adjustColor)
                {
                    textItem.Foreground = ItemColors.Black;
                }
            }

            // lines
            foreach (var lineItem in frame.Lines)
            {
                if (adjustThickness)
                {
                    lineItem.StrokeThickness = 0.018 * 72.0 / 2.54; // 0.18mm 
                }

                if (adjustColor)
                {
                    lineItem.Stroke = ItemColors.Black;
                }
            }

            return frame;
        }

        private BlockItem CreatePage(BlockItem content, bool enableFrame, bool enableGrid)
        {
            var page = new BlockItem();
            page.Init(-1, 0.0, 0.0, 0.0, 0.0, -1, "PAGE");

            if (enableGrid)
            {
                var grid = CreateGridBlock(GridBlock, true, false);
                page.Blocks.Add(grid);
            }

            if (enableFrame)
            {
                var frame = CreateFrameBlock(FrameBlock, true, true);
                page.Blocks.Add(frame);
            }

            page.Blocks.Add(content);

            return page;
        }

        #endregion

        #region Plugins

        private void CreatePlugins()
        {
            InvertLineStartPlugin = new InvertLineStartPlugin(_serviceLocator);
            InvertLineEndPlugin = new InvertLineEndPlugin(_serviceLocator);
        }

        private void ProcessPlugin(ISelectedBlockPlugin plugin)
        {
            if (plugin.CanProcess(ContentSheet, ContentBlock, SelectedBlock, Options))
            {
                var selectedBlock = _blockController.ShallowCopy(SelectedBlock);

                FinishEdit();
                HistoryController.Register(plugin.Name);

                plugin.Process(ContentSheet, ContentBlock, selectedBlock, Options);

                SelectedBlock = selectedBlock;
            }
        }

        public void InvertSelectedLineStart()
        {
            ProcessPlugin(InvertLineStartPlugin);
        }

        public void InvertSelectedLineEnd()
        {
            ProcessPlugin(InvertLineEndPlugin);
        }

        #endregion
    }
}

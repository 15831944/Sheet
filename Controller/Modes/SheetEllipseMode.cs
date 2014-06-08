﻿using Sheet.Block;
using Sheet.Block.Core;
using Sheet.Controller.Core;
using Sheet.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet.Controller.Modes
{
    public class SheetEllipseMode
    {
        #region IoC

        private readonly ISheetController _sheetController;
        private readonly IServiceLocator _serviceLocator;
        private readonly IBlockController _blockController;
        private readonly IBlockFactory _blockFactory;
        private readonly IBlockHelper _blockHelper;
        private readonly IItemController _itemController;
        private readonly IPointController _pointController;

        public SheetEllipseMode(ISheetController sheetController, IServiceLocator serviceLocator)
        {
            this._serviceLocator = serviceLocator;
            this._sheetController = sheetController;
            this._blockController = serviceLocator.GetInstance<IBlockController>();
            this._blockFactory = serviceLocator.GetInstance<IBlockFactory>();
            this._blockHelper = serviceLocator.GetInstance<IBlockHelper>();
            this._itemController = serviceLocator.GetInstance<IItemController>();
            this._pointController = serviceLocator.GetInstance<IPointController>();
        }

        #endregion

        #region Fields

        private IEllipse TempEllipse;
        private ImmutablePoint SelectionStartPoint;

        #endregion

        #region Methods

        public void Init(ImmutablePoint p)
        {
            double x = _itemController.Snap(p.X, _sheetController.Options.SnapSize);
            double y = _itemController.Snap(p.Y, _sheetController.Options.SnapSize);
            SelectionStartPoint = new ImmutablePoint(x, y);
            TempEllipse = _blockFactory.CreateEllipse(_sheetController.Options.LineThickness / _sheetController.ZoomController.Zoom, x, y, 0.0, 0.0, false, ItemColors.Black, ItemColors.Transparent);
            _sheetController.OverlaySheet.Add(TempEllipse);
            _sheetController.OverlaySheet.Capture();
        }

        public void Move(ImmutablePoint p)
        {
            double sx = SelectionStartPoint.X;
            double sy = SelectionStartPoint.Y;
            double x = _itemController.Snap(p.X, _sheetController.Options.SnapSize);
            double y = _itemController.Snap(p.Y, _sheetController.Options.SnapSize);
            _blockHelper.SetLeft(TempEllipse, Math.Min(sx, x));
            _blockHelper.SetTop(TempEllipse, Math.Min(sy, y));
            _blockHelper.SetWidth(TempEllipse, Math.Abs(sx - x));
            _blockHelper.SetHeight(TempEllipse, Math.Abs(sy - y));
        }

        public void Finish()
        {
            double x = _blockHelper.GetLeft(TempEllipse);
            double y = _blockHelper.GetTop(TempEllipse);
            double width = _blockHelper.GetWidth(TempEllipse);
            double height = _blockHelper.GetHeight(TempEllipse);
            if (width == 0.0 || height == 0.0)
            {
                Cancel();
            }
            else
            {
                _sheetController.OverlaySheet.ReleaseCapture();
                _sheetController.OverlaySheet.Remove(TempEllipse);
                _sheetController.HistoryController.Register("Create Ellipse");
                _sheetController.GetContent().Ellipses.Add(TempEllipse);
                _sheetController.ContentSheet.Add(TempEllipse);
                TempEllipse = null;
            }
        }

        public void Cancel()
        {
            _sheetController.OverlaySheet.ReleaseCapture();
            _sheetController.OverlaySheet.Remove(TempEllipse);
            TempEllipse = null;
        }

        public void Reset()
        {
            if (TempEllipse != null)
            {
                _sheetController.OverlaySheet.Remove(TempEllipse);
                TempEllipse = null;
            }
        }

        public void Adjust(double zoom)
        {
            double lineThicknessZoomed = _sheetController.Options.LineThickness / zoom;

            if (TempEllipse != null)
            {
                _blockHelper.SetStrokeThickness(TempEllipse, lineThicknessZoomed);
            }
        }

        public void ToggleFill()
        {
            if (TempEllipse != null)
            {
                _blockController.ToggleFill(TempEllipse);
            }
        }

        #endregion
    }
}

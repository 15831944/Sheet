﻿using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sheet
{
    #region Block Model

    public enum Mode
    {
        None,
        Selection,
        Insert,
        Pan,
        Move,
        Edit,
        Line,
        Rectangle,
        Ellipse,
        Text,
        Image,
        TextEditor
    }

    public class SheetOptions
    {
        public double PageOriginX { get; set; }
        public double PageOriginY { get; set; }
        public double PageWidth { get; set; }
        public double PageHeight { get; set; }
        public double SnapSize { get; set; }
        public double GridSize { get; set; }
        public double FrameThickness { get; set; }
        public double GridThickness { get; set; }
        public double SelectionThickness { get; set; }
        public double LineThickness { get; set; }
        public double HitTestSize { get; set; }
        public int DefaultZoomIndex { get; set; }
        public int MaxZoomIndex { get; set; }
        public double[] ZoomFactors { get; set; }
    }

    public interface ISheet<T> where T : class
    {
        T GetParent();
        void Add(T element);
        void Remove(T element);
        void Capture();
        void ReleaseCapture();
        bool IsCaptured { get; }
    }

    public class Block
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Color Backgroud { get; set; }
        public int DataId { get; set; }
        public List<Line> Lines { get; set; }
        public List<Rectangle> Rectangles { get; set; }
        public List<Ellipse> Ellipses { get; set; }
        public List<Grid> Texts { get; set; }
        public List<Image> Images { get; set; }
        public List<Block> Blocks { get; set; }
        public Block(int id, double x, double y, int dataId, string name)
        {
            Id = id;
            X = x;
            Y = y;
            DataId = dataId;
            Name = name;
        }
        public void Init()
        {
            Lines = new List<Line>();
            Rectangles = new List<Rectangle>();
            Ellipses = new List<Ellipse>();
            Texts = new List<Grid>();
            Images = new List<Image>();
            Blocks = new List<Block>();
        }
        public void ReInit()
        {
            if (Lines == null)
            {
                Lines = new List<Line>();
            }

            if (Rectangles == null)
            {
                Rectangles = new List<Rectangle>();
            }

            if (Ellipses == null)
            {
                Ellipses = new List<Ellipse>();
            }

            if (Texts == null)
            {
                Texts = new List<Grid>();
            }

            if (Images == null)
            {
                Images = new List<Image>();
            }

            if (Blocks == null)
            {
                Blocks = new List<Block>();
            }
        }
    }

    public class CanvasSheet : ISheet<FrameworkElement>
    {
        #region Fields

        private Canvas canvas = null;

        #endregion

        #region Constructor

        public CanvasSheet(Canvas canvas)
        {
            this.canvas = canvas;
        }

        #endregion

        #region ISheet

        public FrameworkElement GetParent()
        {
            return canvas;
        }

        public void Add(FrameworkElement element)
        {
            canvas.Children.Add(element);
        }

        public void Remove(FrameworkElement element)
        {
            canvas.Children.Remove(element);
        }

        public void Capture()
        {
            canvas.CaptureMouse();
        }

        public void ReleaseCapture()
        {
            canvas.ReleaseMouseCapture();
        }

        public bool IsCaptured
        {
            get { return canvas.IsMouseCaptured; }
        }

        #endregion
    }

    #endregion

    #region Block Serializer

    public static class BlockSerializer
    {
        #region Serialize

        private static ItemColor ToItemColor(Brush brush)
        {
            var color = (brush as SolidColorBrush).Color;
            return ToItemColor(color);
        }

        private static ItemColor ToItemColor(Color color)
        {
            return new ItemColor()
            {
                Alpha = color.A,
                Red = color.R,
                Green = color.G,
                Blue = color.B
            };
        }

        public static LineItem SerializeLine(Line line)
        {
            var lineItem = new LineItem();

            lineItem.Id = 0;
            lineItem.X1 = line.X1;
            lineItem.Y1 = line.Y1;
            lineItem.X2 = line.X2;
            lineItem.Y2 = line.Y2;
            lineItem.Stroke = ToItemColor(line.Stroke);

            return lineItem;
        }

        public static RectangleItem SerializeRectangle(Rectangle rectangle)
        {
            var rectangleItem = new RectangleItem();

            rectangleItem.Id = 0;
            rectangleItem.X = Canvas.GetLeft(rectangle);
            rectangleItem.Y = Canvas.GetTop(rectangle);
            rectangleItem.Width = rectangle.Width;
            rectangleItem.Height = rectangle.Height;
            rectangleItem.IsFilled = rectangle.Fill == BlockFactory.TransparentBrush ? false : true;
            rectangleItem.Stroke = ToItemColor(rectangle.Stroke);
            rectangleItem.Fill = ToItemColor(rectangle.Fill);

            return rectangleItem;
        }

        public static EllipseItem SerializeEllipse(Ellipse ellipse)
        {
            var ellipseItem = new EllipseItem();

            ellipseItem.Id = 0;
            ellipseItem.X = Canvas.GetLeft(ellipse);
            ellipseItem.Y = Canvas.GetTop(ellipse);
            ellipseItem.Width = ellipse.Width;
            ellipseItem.Height = ellipse.Height;
            ellipseItem.IsFilled = ellipse.Fill == BlockFactory.TransparentBrush ? false : true;
            ellipseItem.Stroke = ToItemColor(ellipse.Stroke);
            ellipseItem.Fill = ToItemColor(ellipse.Fill);

            return ellipseItem;
        }

        public static TextItem SerializeText(Grid text)
        {
            var textItem = new TextItem();

            textItem.Id = 0;
            textItem.X = Canvas.GetLeft(text);
            textItem.Y = Canvas.GetTop(text);
            textItem.Width = text.Width;
            textItem.Height = text.Height;

            var tb = BlockFactory.GetTextBlock(text);
            textItem.Text = tb.Text;
            textItem.HAlign = (int)tb.HorizontalAlignment;
            textItem.VAlign = (int)tb.VerticalAlignment;
            textItem.Size = tb.FontSize;
            textItem.Foreground = ToItemColor(tb.Foreground);
            textItem.Backgroud = ToItemColor(tb.Background);

            return textItem;
        }

        public static ImageItem SerializeImage(Image image)
        {
            var imageItem = new ImageItem();

            imageItem.Id = 0;
            imageItem.X = Canvas.GetLeft(image);
            imageItem.Y = Canvas.GetTop(image);
            imageItem.Width = image.Width;
            imageItem.Height = image.Height;
            imageItem.Data = image.Tag as byte[];

            return imageItem;
        }

        public static BlockItem SerializeBlock(Block parent)
        {
            var blockItem = new BlockItem();
            blockItem.Init(0, parent.X, parent.Y, parent.DataId, parent.Name);
            blockItem.Width = 0;
            blockItem.Height = 0;
            blockItem.Backgroud = ToItemColor(parent.Backgroud);

            foreach (var line in parent.Lines)
            {
                blockItem.Lines.Add(SerializeLine(line));
            }

            foreach (var rectangle in parent.Rectangles)
            {
                blockItem.Rectangles.Add(SerializeRectangle(rectangle));
            }

            foreach (var ellipse in parent.Ellipses)
            {
                blockItem.Ellipses.Add(SerializeEllipse(ellipse));
            }

            foreach (var text in parent.Texts)
            {
                blockItem.Texts.Add(SerializeText(text));
            }

            foreach (var image in parent.Images)
            {
                blockItem.Images.Add(SerializeImage(image));
            }

            foreach (var block in parent.Blocks)
            {
                blockItem.Blocks.Add(SerializeBlock(block));
            }

            return blockItem;
        }

        public static BlockItem SerializerBlockContents(Block parent, int id, double x, double y, int dataId, string name)
        {
            var lines = parent.Lines;
            var rectangles = parent.Rectangles;
            var ellipses = parent.Ellipses;
            var texts = parent.Texts;
            var images = parent.Images;
            var blocks = parent.Blocks;

            var sheet = new BlockItem() { Backgroud = new ItemColor() };
            sheet.Init(id, x, y, dataId, name);

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    sheet.Lines.Add(SerializeLine(line));
                }
            }

            if (rectangles != null)
            {
                foreach (var rectangle in rectangles)
                {
                    sheet.Rectangles.Add(SerializeRectangle(rectangle));
                }
            }

            if (ellipses != null)
            {
                foreach (var ellipse in ellipses)
                {
                    sheet.Ellipses.Add(SerializeEllipse(ellipse));
                }
            }

            if (texts != null)
            {
                foreach (var text in texts)
                {
                    sheet.Texts.Add(SerializeText(text));
                }
            }

            if (images != null)
            {
                foreach (var image in images)
                {
                    sheet.Images.Add(SerializeImage(image));
                }
            }

            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    sheet.Blocks.Add(SerializeBlock(block));
                }
            }

            return sheet;
        }

        #endregion

        #region Deserialize

        public static Line DeserializeLineItem(ISheet<FrameworkElement> sheet, Block parent, LineItem lineItem, double thickness)
        {
            var line = BlockFactory.CreateLine(thickness, lineItem.X1, lineItem.Y1, lineItem.X2, lineItem.Y2, BlockFactory.NormalBrush);

            if (parent != null)
            {
                parent.Lines.Add(line);
            }

            if (sheet != null)
            {
                sheet.Add(line);
            }

            return line;
        }

        public static Rectangle DeserializeRectangleItem(ISheet<FrameworkElement> sheet, Block parent, RectangleItem rectangleItem, double thickness)
        {
            var rectangle = BlockFactory.CreateRectangle(thickness, rectangleItem.X, rectangleItem.Y, rectangleItem.Width, rectangleItem.Height, rectangleItem.IsFilled);

            if (parent != null)
            {
                parent.Rectangles.Add(rectangle);
            }

            if (sheet != null)
            {
                sheet.Add(rectangle);
            }

            return rectangle;
        }

        public static Ellipse DeserializeEllipseItem(ISheet<FrameworkElement> sheet, Block parent, EllipseItem ellipseItem, double thickness)
        {
            var ellipse = BlockFactory.CreateEllipse(thickness, ellipseItem.X, ellipseItem.Y, ellipseItem.Width, ellipseItem.Height, ellipseItem.IsFilled);

            if (parent != null)
            {
                parent.Ellipses.Add(ellipse);
            }

            if (sheet != null)
            {
                sheet.Add(ellipse);
            }

            return ellipse;
        }

        public static Grid DeserializeTextItem(ISheet<FrameworkElement> sheet, Block parent, TextItem textItem)
        {
            var text = BlockFactory.CreateText(textItem.Text,
                textItem.X, textItem.Y,
                textItem.Width, textItem.Height,
                (HorizontalAlignment)textItem.HAlign,
                (VerticalAlignment)textItem.VAlign,
                textItem.Size,
                BlockFactory.TransparentBrush,
                BlockFactory.NormalBrush);

            if (parent != null)
            {
                parent.Texts.Add(text);
            }

            if (sheet != null)
            {
                sheet.Add(text);
            }

            return text;
        }

        public static Image DeserializeImageItem(ISheet<FrameworkElement> sheet, Block parent, ImageItem imageItem)
        {
            var image = BlockFactory.CreateImage(imageItem.X, imageItem.Y, imageItem.Width, imageItem.Height, imageItem.Data);

            if (parent != null)
            {
                parent.Images.Add(image);
            }

            if (sheet != null)
            {
                sheet.Add(image);
            }

            return image;
        }

        public static Block DeserializeBlockItem(ISheet<FrameworkElement> sheet, Block parent, BlockItem blockItem, bool select, double thickness)
        {
            var block = new Block(blockItem.Id, blockItem.X, blockItem.Y, blockItem.DataId, blockItem.Name);
            block.Init();

            foreach (var textItem in blockItem.Texts)
            {
                var text = DeserializeTextItem(sheet, block, textItem);

                if (select)
                {
                    BlockController.SelectText(text);
                }
            }

            foreach (var imageItem in blockItem.Images)
            {
                var image = DeserializeImageItem(sheet, block, imageItem);

                if (select)
                {
                    BlockController.SelectImage(image);
                }
            }

            foreach (var lineItem in blockItem.Lines)
            {
                var line = DeserializeLineItem(sheet, block, lineItem, thickness);

                if (select)
                {
                    BlockController.SelectLine(line);
                }
            }

            foreach (var rectangleItem in blockItem.Rectangles)
            {
                var rectangle = DeserializeRectangleItem(sheet, block, rectangleItem, thickness);

                if (select)
                {
                    BlockController.SelectRectangle(rectangle);
                }
            }

            foreach (var ellipseItem in blockItem.Ellipses)
            {
                var ellipse = DeserializeEllipseItem(sheet, block, ellipseItem, thickness);

                if (select)
                {
                    BlockController.SelectEllipse(ellipse);
                }
            }

            foreach (var childBlockItem in blockItem.Blocks)
            {
                DeserializeBlockItem(sheet, block, childBlockItem, select, thickness);
            }

            if (parent != null)
            {
                parent.Blocks.Add(block);
            }

            return block;
        }

        #endregion
    }

    #endregion

    #region Block Controller

    public static class BlockController
    {
        #region Add

        public static void AddLines(ISheet<FrameworkElement> sheet, IEnumerable<LineItem> lineItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Lines = new List<Line>();
            }

            foreach (var lineItem in lineItems)
            {
                var line = BlockSerializer.DeserializeLineItem(sheet, parent, lineItem, thickness);

                if (select)
                {
                    SelectLine(line);
                    selected.Lines.Add(line);
                }
            }
        }

        public static void AddRectangles(ISheet<FrameworkElement> sheet, IEnumerable<RectangleItem> rectangleItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Rectangles = new List<Rectangle>();
            }

            foreach (var rectangleItem in rectangleItems)
            {
                var rectangle = BlockSerializer.DeserializeRectangleItem(sheet, parent, rectangleItem, thickness);

                if (select)
                {
                    SelectRectangle(rectangle);
                    selected.Rectangles.Add(rectangle);
                }
            }
        }

        public static void AddEllipses(ISheet<FrameworkElement> sheet, IEnumerable<EllipseItem> ellipseItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Ellipses = new List<Ellipse>();
            }

            foreach (var ellipseItem in ellipseItems)
            {
                var ellipse = BlockSerializer.DeserializeEllipseItem(sheet, parent, ellipseItem, thickness);

                if (select)
                {
                    SelectEllipse(ellipse);
                    selected.Ellipses.Add(ellipse);
                }
            }
        }

        public static void AddTexts(ISheet<FrameworkElement> sheet, IEnumerable<TextItem> textItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Texts = new List<Grid>();
            }

            foreach (var textItem in textItems)
            {
                var text = BlockSerializer.DeserializeTextItem(sheet, parent, textItem);

                if (select)
                {
                    SelectText(text);
                    selected.Texts.Add(text);
                }
            }
        }

        public static void AddImages(ISheet<FrameworkElement> sheet, IEnumerable<ImageItem> imageItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Images = new List<Image>();
            }

            foreach (var imageItem in imageItems)
            {
                var image = BlockSerializer.DeserializeImageItem(sheet, parent, imageItem);

                if (select)
                {
                    SelectImage(image);
                    selected.Images.Add(image);
                }
            }
        }

        public static void AddBlocks(ISheet<FrameworkElement> sheet, IEnumerable<BlockItem> blockItems, Block parent, Block selected, bool select, double thickness)
        {
            if (select)
            {
                selected.Blocks = new List<Block>();
            }

            foreach (var blockItem in blockItems)
            {
                var block = BlockSerializer.DeserializeBlockItem(sheet, parent, blockItem, select, thickness);

                if (select)
                {
                    selected.Blocks.Add(block);
                }
            }
        }

        public static void AddBlockContents(ISheet<FrameworkElement> sheet, BlockItem blockItem, Block logic, Block selected, bool select, double thickness)
        {
            if (blockItem != null)
            {
                AddTexts(sheet, blockItem.Texts, logic, selected, select, thickness);
                AddImages(sheet, blockItem.Images, logic, selected, select, thickness);
                AddLines(sheet, blockItem.Lines, logic, selected, select, thickness);
                AddRectangles(sheet, blockItem.Rectangles, logic, selected, select, thickness);
                AddEllipses(sheet, blockItem.Ellipses, logic, selected, select, thickness);
                AddBlocks(sheet, blockItem.Blocks, logic, selected, select, thickness);
            }
        }

        public static void AddBrokenBlock(ISheet<FrameworkElement> sheet, BlockItem blockItem, Block logic, Block selected, bool select, double thickness)
        {
            AddTexts(sheet, blockItem.Texts, logic, selected, select, thickness);
            AddImages(sheet, blockItem.Images, logic, selected, select, thickness);
            AddLines(sheet, blockItem.Lines, logic, selected, select, thickness);
            AddRectangles(sheet, blockItem.Rectangles, logic, selected, select, thickness);
            AddEllipses(sheet, blockItem.Ellipses, logic, selected, select, thickness);

            foreach (var block in blockItem.Blocks)
            {
                AddTexts(sheet, block.Texts, logic, selected, select, thickness);
                AddImages(sheet, block.Images, logic, selected, select, thickness);
                AddLines(sheet, block.Lines, logic, selected, select, thickness);
                AddRectangles(sheet, block.Rectangles, logic, selected, select, thickness);
                AddEllipses(sheet, block.Ellipses, logic, selected, select, thickness);
                AddBlocks(sheet, block.Blocks, logic, selected, select, thickness);
            }
        }

        #endregion

        #region Remove

        public static void RemoveLines(ISheet<FrameworkElement> sheet, IEnumerable<Line> lines)
        {
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    sheet.Remove(line);
                }
            }
        }

        public static void RemoveRectangles(ISheet<FrameworkElement> sheet, IEnumerable<Rectangle> rectangles)
        {
            if (rectangles != null)
            {
                foreach (var rectangle in rectangles)
                {
                    sheet.Remove(rectangle);
                }
            }
        }

        public static void RemoveEllipses(ISheet<FrameworkElement> sheet, IEnumerable<Ellipse> ellipses)
        {
            if (ellipses != null)
            {
                foreach (var ellipse in ellipses)
                {
                    sheet.Remove(ellipse);
                }
            }
        }

        public static void RemoveTexts(ISheet<FrameworkElement> sheet, IEnumerable<Grid> texts)
        {
            if (texts != null)
            {
                foreach (var text in texts)
                {
                    sheet.Remove(text);
                }
            }
        }

        public static void RemoveImages(ISheet<FrameworkElement> sheet, IEnumerable<Image> images)
        {
            if (images != null)
            {
                foreach (var image in images)
                {
                    sheet.Remove(image);
                }
            }
        }

        public static void RemoveBlocks(ISheet<FrameworkElement> sheet, IEnumerable<Block> blocks)
        {
            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    RemoveBlock(sheet, block);
                }
            }
        }

        public static void RemoveBlock(ISheet<FrameworkElement> sheet, Block block)
        {
            RemoveLines(sheet, block.Lines);
            RemoveRectangles(sheet, block.Rectangles);
            RemoveEllipses(sheet, block.Ellipses);
            RemoveTexts(sheet, block.Texts);
            RemoveImages(sheet, block.Images);
            RemoveBlocks(sheet, block.Blocks);
        }

        public static void RemoveSelectedFromBlock(ISheet<FrameworkElement> sheet, Block parent, Block selected)
        {
            if (selected.Lines != null)
            {
                RemoveLines(sheet, selected.Lines);

                foreach (var line in selected.Lines)
                {
                    parent.Lines.Remove(line);
                }

                selected.Lines = null;
            }

            if (selected.Rectangles != null)
            {
                RemoveRectangles(sheet, selected.Rectangles);

                foreach (var rectangle in selected.Rectangles)
                {
                    parent.Rectangles.Remove(rectangle);
                }

                selected.Rectangles = null;
            }

            if (selected.Ellipses != null)
            {
                RemoveEllipses(sheet, selected.Ellipses);

                foreach (var ellipse in selected.Ellipses)
                {
                    parent.Ellipses.Remove(ellipse);
                }

                selected.Ellipses = null;
            }

            if (selected.Texts != null)
            {
                RemoveTexts(sheet, selected.Texts);

                foreach (var text in selected.Texts)
                {
                    parent.Texts.Remove(text);
                }

                selected.Texts = null;
            }

            if (selected.Images != null)
            {
                RemoveImages(sheet, selected.Images);

                foreach (var image in selected.Images)
                {
                    parent.Images.Remove(image);
                }

                selected.Images = null;
            }

            if (selected.Blocks != null)
            {
                foreach (var block in selected.Blocks)
                {
                    RemoveLines(sheet, block.Lines);
                    RemoveRectangles(sheet, block.Rectangles);
                    RemoveEllipses(sheet, block.Ellipses);
                    RemoveTexts(sheet, block.Texts);
                    RemoveImages(sheet, block.Images);
                    RemoveBlocks(sheet, block.Blocks);

                    parent.Blocks.Remove(block);
                }

                selected.Blocks = null;
            }
        }

        #endregion

        #region Move

        public static void MoveLines(double x, double y, IEnumerable<Line> lines)
        {
            foreach (var line in lines)
            {
                line.X1 += x;
                line.Y1 += y;
                line.X2 += x;
                line.Y2 += y;
            }
        }

        public static void MoveRectangles(double x, double y, IEnumerable<Rectangle> rectangles)
        {
            foreach (var rectangle in rectangles)
            {
                Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) + x);
                Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) + y);
            }
        }

        public static void MoveEllipses(double x, double y, IEnumerable<Ellipse> ellipses)
        {
            foreach (var ellipse in ellipses)
            {
                Canvas.SetLeft(ellipse, Canvas.GetLeft(ellipse) + x);
                Canvas.SetTop(ellipse, Canvas.GetTop(ellipse) + y);
            }
        }

        public static void MoveTexts(double x, double y, IEnumerable<Grid> texts)
        {
            foreach (var text in texts)
            {
                Canvas.SetLeft(text, Canvas.GetLeft(text) + x);
                Canvas.SetTop(text, Canvas.GetTop(text) + y);
            }
        }

        public static void MoveImages(double x, double y, IEnumerable<Image> images)
        {
            foreach (var image in images)
            {
                Canvas.SetLeft(image, Canvas.GetLeft(image) + x);
                Canvas.SetTop(image, Canvas.GetTop(image) + y);
            }
        }

        public static void MoveBlocks(double x, double y, IEnumerable<Block> blocks)
        {
            foreach (var block in blocks)
            {
                MoveLines(x, y, block.Lines);
                MoveRectangles(x, y, block.Rectangles);
                MoveEllipses(x, y, block.Ellipses);
                MoveTexts(x, y, block.Texts);
                MoveImages(x, y, block.Images);
                MoveBlocks(x, y, block.Blocks);
            }
        }

        public static void Move(double x, double y, Block block)
        {
            if (block.Lines != null)
            {
                MoveLines(x, y, block.Lines);
            }

            if (block.Rectangles != null)
            {
                MoveRectangles(x, y, block.Rectangles);
            }

            if (block.Ellipses != null)
            {
                MoveEllipses(x, y, block.Ellipses);
            }

            if (block.Texts != null)
            {
                MoveTexts(x, y, block.Texts);
            }

            if (block.Images != null)
            {
                MoveImages(x, y, block.Images);
            }

            if (block.Blocks != null)
            {
                MoveBlocks(x, y, block.Blocks);
            }
        }

        #endregion

        #region Select

        private static int DeselectedZIndex = 0;
        private static int SelectedZIndex = 1;

        public static void DeselectLine(Line line)
        {
            line.Stroke = BlockFactory.NormalBrush;
            Panel.SetZIndex(line, DeselectedZIndex);
        }

        public static void DeselectRectangle(Rectangle rectangle)
        {
            rectangle.Stroke = BlockFactory.NormalBrush;
            rectangle.Fill = rectangle.Fill == BlockFactory.TransparentBrush ? BlockFactory.TransparentBrush : BlockFactory.NormalBrush;
            Panel.SetZIndex(rectangle, DeselectedZIndex);
        }

        public static void DeselectEllipse(Ellipse ellipse)
        {
            ellipse.Stroke = BlockFactory.NormalBrush;
            ellipse.Fill = ellipse.Fill == BlockFactory.TransparentBrush ? BlockFactory.TransparentBrush : BlockFactory.NormalBrush;
            Panel.SetZIndex(ellipse, DeselectedZIndex);
        }

        public static void DeselectText(Grid text)
        {
            BlockFactory.GetTextBlock(text).Foreground = BlockFactory.NormalBrush;
            Panel.SetZIndex(text, DeselectedZIndex);
        }

        public static void DeselectImage(Image image)
        {
            image.OpacityMask = BlockFactory.NormalBrush;
            Panel.SetZIndex(image, DeselectedZIndex);
        }

        public static void DeselectBlock(Block parent)
        {
            if (parent.Lines != null)
            {
                foreach (var line in parent.Lines)
                {
                    DeselectLine(line);
                }
            }

            if (parent.Rectangles != null)
            {
                foreach (var rectangle in parent.Rectangles)
                {
                    DeselectRectangle(rectangle);
                }
            }

            if (parent.Ellipses != null)
            {
                foreach (var ellipse in parent.Ellipses)
                {
                    DeselectEllipse(ellipse);
                }
            }

            if (parent.Texts != null)
            {
                foreach (var text in parent.Texts)
                {
                    DeselectText(text);
                }
            }

            if (parent.Images != null)
            {
                foreach (var image in parent.Images)
                {
                    DeselectImage(image);
                }
            }

            if (parent.Blocks != null)
            {
                foreach (var block in parent.Blocks)
                {
                    DeselectBlock(block);
                }
            }
        }

        public static void SelectLine(Line line)
        {
            line.Stroke = BlockFactory.SelectedBrush;
            Panel.SetZIndex(line, SelectedZIndex);
        }

        public static void SelectRectangle(Rectangle rectangle)
        {
            rectangle.Stroke = BlockFactory.SelectedBrush;
            rectangle.Fill = rectangle.Fill == BlockFactory.TransparentBrush ? BlockFactory.TransparentBrush : BlockFactory.SelectedBrush;
            Panel.SetZIndex(rectangle, SelectedZIndex);
        }

        public static void SelectEllipse(Ellipse ellipse)
        {
            ellipse.Stroke = BlockFactory.SelectedBrush;
            ellipse.Fill = ellipse.Fill == BlockFactory.TransparentBrush ? BlockFactory.TransparentBrush : BlockFactory.SelectedBrush;
            Panel.SetZIndex(ellipse, SelectedZIndex);
        }

        public static void SelectText(Grid text)
        {
            BlockFactory.GetTextBlock(text).Foreground = BlockFactory.SelectedBrush;
            Panel.SetZIndex(text, SelectedZIndex);
        }

        public static void SelectImage(Image image)
        {
            image.OpacityMask = BlockFactory.SelectedBrush;
            Panel.SetZIndex(image, SelectedZIndex);
        }

        public static void SelectBlock(Block parent)
        {
            foreach (var line in parent.Lines)
            {
                SelectLine(line);
            }

            foreach (var rectangle in parent.Rectangles)
            {
                SelectRectangle(rectangle);
            }

            foreach (var ellipse in parent.Ellipses)
            {
                SelectEllipse(ellipse);
            }

            foreach (var text in parent.Texts)
            {
                SelectText(text);
            }

            foreach (var image in parent.Images)
            {
                SelectImage(image);
            }

            foreach (var block in parent.Blocks)
            {
                SelectBlock(block);
            }
        }

        public static void SelectAll(Block selected, Block logic)
        {
            selected.Init();

            foreach (var line in logic.Lines)
            {
                SelectLine(line);
                selected.Lines.Add(line);
            }

            foreach (var rectangle in logic.Rectangles)
            {
                SelectRectangle(rectangle);
                selected.Rectangles.Add(rectangle);
            }

            foreach (var ellipse in logic.Ellipses)
            {
                SelectEllipse(ellipse);
                selected.Ellipses.Add(ellipse);
            }

            foreach (var text in logic.Texts)
            {
                SelectText(text);
                selected.Texts.Add(text);
            }

            foreach (var image in logic.Images)
            {
                SelectImage(image);
                selected.Images.Add(image);
            }

            foreach (var parent in logic.Blocks)
            {
                foreach (var line in parent.Lines)
                {
                    SelectLine(line);
                }

                foreach (var rectangle in parent.Rectangles)
                {
                    SelectRectangle(rectangle);
                }

                foreach (var ellipse in parent.Ellipses)
                {
                    SelectEllipse(ellipse);
                }

                foreach (var text in parent.Texts)
                {
                    SelectText(text);
                }

                foreach (var image in parent.Images)
                {
                    SelectImage(image);
                }

                foreach (var block in parent.Blocks)
                {
                    SelectBlock(block);
                }

                selected.Blocks.Add(parent);
            }
        }

        public static void DeselectAll(Block selected)
        {
            if (selected.Lines != null)
            {
                foreach (var line in selected.Lines)
                {
                    DeselectLine(line);
                }

                selected.Lines = null;
            }

            if (selected.Rectangles != null)
            {
                foreach (var rectangle in selected.Rectangles)
                {
                    DeselectRectangle(rectangle);
                }

                selected.Rectangles = null;
            }

            if (selected.Ellipses != null)
            {
                foreach (var ellipse in selected.Ellipses)
                {
                    DeselectEllipse(ellipse);
                }

                selected.Ellipses = null;
            }

            if (selected.Texts != null)
            {
                foreach (var text in selected.Texts)
                {
                    DeselectText(text);
                }

                selected.Texts = null;
            }

            if (selected.Images != null)
            {
                foreach (var image in selected.Images)
                {
                    DeselectImage(image);
                }

                selected.Images = null;
            }

            if (selected.Blocks != null)
            {
                foreach (var parent in selected.Blocks)
                {
                    foreach (var line in parent.Lines)
                    {
                        DeselectLine(line);
                    }

                    foreach (var rectangle in parent.Rectangles)
                    {
                        DeselectRectangle(rectangle);
                    }

                    foreach (var ellipse in parent.Ellipses)
                    {
                        DeselectEllipse(ellipse);
                    }

                    foreach (var text in parent.Texts)
                    {
                        DeselectText(text);
                    }

                    foreach (var image in parent.Images)
                    {
                        DeselectImage(image);
                    }

                    foreach (var block in parent.Blocks)
                    {
                        DeselectBlock(block);
                    }
                }

                selected.Blocks = null;
            }
        }

        public static bool HaveSelected(Block selected)
        {
            return (selected.Lines != null
                || selected.Rectangles != null
                || selected.Ellipses != null
                || selected.Texts != null
                || selected.Images != null
                || selected.Blocks != null);
        }

        public static bool HaveOneLineSelected(Block selected)
        {
            return (selected.Lines != null
                && selected.Lines.Count == 1
                && selected.Rectangles == null
                && selected.Ellipses == null
                && selected.Texts == null
                && selected.Images == null
                && selected.Blocks == null);
        }

        public static bool HaveOneRectangleSelected(Block selected)
        {
            return (selected.Lines == null
                && selected.Rectangles != null
                && selected.Rectangles.Count == 1
                && selected.Ellipses == null
                && selected.Texts == null
                && selected.Images == null
                && selected.Blocks == null);
        }

        public static bool HaveOneEllipseSelected(Block selected)
        {
            return (selected.Lines == null
                && selected.Rectangles == null
                && selected.Ellipses != null
                && selected.Ellipses.Count == 1
                && selected.Texts == null
                && selected.Images == null
                && selected.Blocks == null);
        }

        public static bool HaveOneTextSelected(Block selected)
        {
            return (selected.Lines == null
                && selected.Rectangles == null
                && selected.Ellipses == null
                && selected.Texts != null
                && selected.Texts.Count == 1
                && selected.Images == null
                && selected.Blocks == null);
        }

        public static bool HaveOneImageSelected(Block selected)
        {
            return (selected.Lines == null
                && selected.Rectangles == null
                && selected.Ellipses == null
                && selected.Texts == null
                && selected.Images != null
                && selected.Images.Count == 1
                && selected.Blocks == null);
        }

        public static bool HaveOneBlockSelected(Block selected)
        {
            return (selected.Lines == null
                && selected.Rectangles == null
                && selected.Ellipses == null
                && selected.Texts == null
                && selected.Images == null
                && selected.Blocks != null
                && selected.Blocks.Count == 1);
        }

        #endregion

        #region HitTest

        public static bool HitTestLines(IEnumerable<Line> lines, Block selected, Rect rect, bool onlyFirst, bool select)
        {
            foreach (var line in lines)
            {
                var bounds = VisualTreeHelper.GetContentBounds(line);
                if (rect.IntersectsWith(bounds))
                {
                    if (select)
                    {
                        if (line.Stroke != BlockFactory.SelectedBrush)
                        {
                            SelectLine(line);
                            selected.Lines.Add(line);
                        }
                        else
                        {
                            DeselectLine(line);
                            selected.Lines.Remove(line);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestRectangles(IEnumerable<Rectangle> rectangles, Block selected, Rect rect, bool onlyFirst, bool select, UIElement relative)
        {
            foreach (var rectangle in rectangles)
            {
                var bounds = VisualTreeHelper.GetContentBounds(rectangle);
                var offset = rectangle.TranslatePoint(new Point(0, 0), relative);
                bounds.Offset(offset.X, offset.Y);
                if (rect.IntersectsWith(bounds))
                {
                    if (select)
                    {
                        if (rectangle.Stroke != BlockFactory.SelectedBrush)
                        {
                            SelectRectangle(rectangle);
                            selected.Rectangles.Add(rectangle);
                        }
                        else
                        {
                            DeselectRectangle(rectangle);
                            selected.Rectangles.Remove(rectangle);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestEllipses(IEnumerable<Ellipse> ellipses, Block selected, Rect rect, bool onlyFirst, bool select, UIElement relative)
        {
            foreach (var ellipse in ellipses)
            {
                var bounds = VisualTreeHelper.GetContentBounds(ellipse);
                var offset = ellipse.TranslatePoint(new Point(0, 0), relative);
                bounds.Offset(offset.X, offset.Y);
                if (rect.IntersectsWith(bounds))
                {
                    if (select)
                    {
                        if (ellipse.Stroke != BlockFactory.SelectedBrush)
                        {
                            SelectEllipse(ellipse);
                            selected.Ellipses.Add(ellipse);
                        }
                        else
                        {
                            DeselectEllipse(ellipse);
                            selected.Ellipses.Remove(ellipse);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestTexts(IEnumerable<Grid> texts, Block selected, Rect rect, bool onlyFirst, bool select, UIElement relative)
        {
            foreach (var text in texts)
            {
                var bounds = VisualTreeHelper.GetContentBounds(text);
                var offset = text.TranslatePoint(new Point(0, 0), relative);
                bounds.Offset(offset.X, offset.Y);
                if (rect.IntersectsWith(bounds))
                {
                    if (select)
                    {
                        var tb = BlockFactory.GetTextBlock(text);
                        if (tb.Foreground != BlockFactory.SelectedBrush)
                        {
                            SelectText(text);
                            selected.Texts.Add(text);
                        }
                        else
                        {
                            DeselectText(text);
                            selected.Texts.Remove(text);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestImages(IEnumerable<Image> images, Block selected, Rect rect, bool onlyFirst, bool select, UIElement relative)
        {
            foreach (var image in images)
            {
                var bounds = VisualTreeHelper.GetContentBounds(image);
                var offset = image.TranslatePoint(new Point(0, 0), relative);
                bounds.Offset(offset.X, offset.Y);
                if (rect.IntersectsWith(bounds))
                {
                    if (select)
                    {
                        if (image.OpacityMask != BlockFactory.SelectedBrush)
                        {
                            SelectImage(image);
                            selected.Images.Add(image);
                        }
                        else
                        {
                            DeselectImage(image);
                            selected.Images.Remove(image);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestBlocks(IEnumerable<Block> blocks, Block selected, Rect rect, bool onlyFirst, bool select, bool selectInsideBlock, UIElement relative)
        {
            foreach (var block in blocks)
            {
                bool result = HitTestBlock(block, selected, rect, true, selectInsideBlock, relative);

                if (result)
                {
                    if (select && !selectInsideBlock)
                    {
                        if (!selected.Blocks.Contains(block))
                        {
                            selected.Blocks.Add(block);
                            SelectBlock(block);
                        }
                        else
                        {
                            selected.Blocks.Remove(block);
                            DeselectBlock(block);
                        }
                    }

                    if (onlyFirst)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HitTestBlock(Block parent, Block selected, Rect rect, bool onlyFirst, bool selectInsideBlock, UIElement relative)
        {
            bool result = false;

            result = HitTestTexts(parent.Texts, selected, rect, onlyFirst, selectInsideBlock, relative);
            if (result && onlyFirst)
            {
                return true;
            }

            result = HitTestImages(parent.Images, selected, rect, onlyFirst, selectInsideBlock, relative);
            if (result && onlyFirst)
            {
                return true;
            }

            result = HitTestLines(parent.Lines, selected, rect, onlyFirst, selectInsideBlock);
            if (result && onlyFirst)
            {
                return true;
            }

            result = HitTestRectangles(parent.Rectangles, selected, rect, onlyFirst, selectInsideBlock, relative);
            if (result && onlyFirst)
            {
                return true;
            }

            result = HitTestEllipses(parent.Ellipses, selected, rect, onlyFirst, selectInsideBlock, relative);
            if (result && onlyFirst)
            {
                return true;
            }

            result = HitTestBlocks(parent.Blocks, selected, rect, onlyFirst, false, selectInsideBlock, relative);
            if (result && onlyFirst)
            {
                return true;
            }

            return false;
        }

        public static bool HitTestClick(ISheet<FrameworkElement> sheet, Block parent, Block selected, Point p, double size, bool selectInsideBlock, bool resetSelected)
        {
            if (resetSelected)
            {
                selected.Init();
            }
            else
            {
                selected.ReInit();
            }

            var rect = new Rect(p.X - size, p.Y - size, 2 * size, 2 * size);

            if (parent.Texts != null)
            {
                bool result = HitTestTexts(parent.Texts, selected, rect, true, true, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            if (parent.Images != null)
            {
                bool result = HitTestImages(parent.Images, selected, rect, true, true, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            if (parent.Lines != null)
            {
                bool result = HitTestLines(parent.Lines, selected, rect, true, true);
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            if (parent.Rectangles != null)
            {
                bool result = HitTestRectangles(parent.Rectangles, selected, rect, true, true, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            if (parent.Ellipses != null)
            {
                bool result = HitTestEllipses(parent.Ellipses, selected, rect, true, true, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            if (parent.Blocks != null)
            {
                bool result = HitTestBlocks(parent.Blocks, selected, rect, true, true, selectInsideBlock, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            HitTestClean(selected);
            return false;
        }

        public static bool HitTestForBlocks(ISheet<FrameworkElement> sheet, Block parent, Block selected, Point p, double size)
        {
            selected.Init();

            var rect = new Rect(p.X - size, p.Y - size, 2 * size, 2 * size);

            if (parent.Blocks != null)
            {
                bool result = HitTestBlocks(parent.Blocks, selected, rect, true, true, false, sheet.GetParent());
                if (result)
                {
                    HitTestClean(selected);
                    return true;
                }
            }

            HitTestClean(selected);
            return false;
        }

        public static void HitTestSelectionRect(ISheet<FrameworkElement> sheet, Block parent, Block selected, Rect rect, bool resetSelected)
        {
            if (resetSelected)
            {
                selected.Init();
            }
            else
            {
                selected.ReInit();
            }

            if (parent.Lines != null)
            {
                HitTestLines(parent.Lines, selected, rect, false, true);
            }

            if (parent.Rectangles != null)
            {
                HitTestRectangles(parent.Rectangles, selected, rect, false, true, sheet.GetParent());
            }

            if (parent.Ellipses != null)
            {
                HitTestEllipses(parent.Ellipses, selected, rect, false, true, sheet.GetParent());
            }

            if (parent.Texts != null)
            {
                HitTestTexts(parent.Texts, selected, rect, false, true, sheet.GetParent());
            }

            if (parent.Images != null)
            {
                HitTestImages(parent.Images, selected, rect, false, true, sheet.GetParent());
            }

            if (parent.Blocks != null)
            {
                HitTestBlocks(parent.Blocks, selected, rect, false, true, false, sheet.GetParent());
            }

            HitTestClean(selected);
        }

        private static void HitTestClean(Block selected)
        {
            if (selected.Lines != null && selected.Lines.Count == 0)
            {
                selected.Lines = null;
            }

            if (selected.Rectangles != null && selected.Rectangles.Count == 0)
            {
                selected.Rectangles = null;
            }

            if (selected.Ellipses != null && selected.Ellipses.Count == 0)
            {
                selected.Ellipses = null;
            }

            if (selected.Texts != null && selected.Texts.Count == 0)
            {
                selected.Texts = null;
            }

            if (selected.Images != null && selected.Images.Count == 0)
            {
                selected.Images = null;
            }

            if (selected.Blocks != null && selected.Blocks.Count == 0)
            {
                selected.Blocks = null;
            }
        }

        #endregion

        #region Fill

        public static void ToggleFill(Rectangle rectangle)
        {
            rectangle.Fill = rectangle.Fill == BlockFactory.TransparentBrush ? BlockFactory.NormalBrush : BlockFactory.TransparentBrush;
        }

        public static void ToggleFill(Ellipse ellipse)
        {
            ellipse.Fill = ellipse.Fill == BlockFactory.TransparentBrush ? BlockFactory.NormalBrush : BlockFactory.TransparentBrush;
        }

        #endregion
    }

    #endregion

    #region Block Factory

    public static class BlockFactory
    {
        #region Brushes

        public static SolidColorBrush NormalBrush = Brushes.Black;
        public static SolidColorBrush SelectedBrush = Brushes.Red;
        public static SolidColorBrush TransparentBrush = Brushes.Transparent;
        public static SolidColorBrush GridBrush = Brushes.LightGray;
        public static SolidColorBrush FrameBrush = Brushes.DarkGray;

        #endregion

        #region Create

        public static Line CreateLine(double thickness, double x1, double y1, double x2, double y2, Brush stroke)
        {
            var line = new Line()
            {
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2
            };

            return line;
        }

        public static Rectangle CreateRectangle(double thickness, double x, double y, double width, double height, bool isFilled)
        {
            var rectangle = new Rectangle()
            {
                Fill = isFilled ? NormalBrush : TransparentBrush,
                Stroke = NormalBrush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = width,
                Height = height
            };

            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);

            return rectangle;
        }

        public static Ellipse CreateEllipse(double thickness, double x, double y, double width, double height, bool isFilled)
        {
            var ellipse = new Ellipse()
            {
                Fill = isFilled ? NormalBrush : TransparentBrush,
                Stroke = NormalBrush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = width,
                Height = height
            };

            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);

            return ellipse;
        }

        public static TextBlock GetTextBlock(Grid text)
        {
            return text.Children[0] as TextBlock;
        }

        public static Grid CreateText(string text,
            double x, double y, double width, double height,
            HorizontalAlignment halign, VerticalAlignment valign,
            double fontSize,
            Brush backgroud, Brush foreground)
        {
            var grid = new Grid();
            grid.Background = backgroud;
            grid.Width = width;
            grid.Height = height;
            Canvas.SetLeft(grid, x);
            Canvas.SetTop(grid, y);

            var tb = new TextBlock();
            tb.HorizontalAlignment = halign;
            tb.VerticalAlignment = valign;
            tb.Background = backgroud;
            tb.Foreground = foreground;
            tb.FontSize = fontSize;
            tb.FontFamily = new FontFamily("Calibri");
            tb.Text = text;

            grid.Children.Add(tb);

            return grid;
        }

        public static Image CreateImage(double x, double y, double width, double height, byte[] data)
        {
            Image image = new Image();

            // enable high quality image scaling
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            // store original image data is Tag property
            image.Tag = data;

            // opacity mask is used for determining selection state
            image.OpacityMask = NormalBrush;

            //using(var ms = new System.IO.MemoryStream(data))
            //{
            //    image = Image.FromStream(ms);
            //}
            using (var ms = new System.IO.MemoryStream(data))
            {
                IBitmap profileImage = BitmapLoader.Current.Load(ms, null, null).Result;
                image.Source = profileImage.ToNative();
            }

            image.Width = width;
            image.Height = height;

            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, y);

            return image;
        }

        #endregion
    }

    #endregion

    #region Page Factory

    public static class PageFactory
    {
        #region Create

        public static void CreateLine(ISheet<FrameworkElement> sheet, List<Line> lines, double thickness, double x1, double y1, double x2, double y2, Brush stroke)
        {
            var line = BlockFactory.CreateLine(thickness, x1, y1, x2, y2, stroke);

            if (lines != null)
            {
                lines.Add(line);
            }

            if (sheet != null)
            {
                sheet.Add(line);
            }
        }

        public static void CreateText(ISheet<FrameworkElement> sheet, List<Grid> texts, string content, double x, double y, double width, double height, HorizontalAlignment halign, VerticalAlignment valign, double size, Brush foreground)
        {
            var text = BlockFactory.CreateText(content, x, y, width, height, halign, valign, size, BlockFactory.TransparentBrush, foreground);

            if (texts != null)
            {
                texts.Add(text);
            }

            if (sheet != null)
            {
                sheet.Add(text);
            }
        }

        public static void CreateFrame(ISheet<FrameworkElement> sheet, Block block, double size, double thickness, Brush stroke)
        {
            double padding = 6.0;
            double width = 1260.0;
            double height = 891.0;
            double startX = padding;
            double startY = padding;
            double rowsStart = 60;
            double rowsEnd = 780.0;

            // frame left rows
            int leftRowNumber = 1;
            for (double y = rowsStart; y < rowsEnd; y += size)
            {
                CreateLine(sheet, block.Lines, thickness, startX, y, 330.0, y, stroke);
                CreateText(sheet, block.Texts, leftRowNumber.ToString("00"), startX, y, 30.0 - padding, size, HorizontalAlignment.Center, VerticalAlignment.Center, 14.0, stroke);
                leftRowNumber++;
            }

            // frame right rows
            int rightRowNumber = 1;
            for (double y = rowsStart; y < rowsEnd; y += size)
            {
                CreateLine(sheet, block.Lines, thickness, 930.0, y, 1260.0 - padding, y, stroke);
                CreateText(sheet, block.Texts, rightRowNumber.ToString("00"), 1260.0 - 30.0, y, 30.0 - padding, size, HorizontalAlignment.Center, VerticalAlignment.Center, 14.0, stroke);
                rightRowNumber++;
            }

            // frame columns
            double[] columnWidth = { 30.0, 210.0, 90.0, 600.0, 210.0, 90.0 };
            double[] columnX = { 30.0, 30.0, startY, startY, 30.0, 30.0 };
            double[] columnY = { rowsEnd, rowsEnd, rowsEnd, rowsEnd, rowsEnd, rowsEnd };

            double start = 0.0;
            for (int i = 0; i < columnWidth.Length; i++)
            {
                start += columnWidth[i];
                CreateLine(sheet, block.Lines, thickness, start, columnX[i], start, columnY[i], stroke);
            }

            // frame header
            CreateLine(sheet, block.Lines, thickness, startX, 30.0, width - padding, 30.0, stroke);

            // frame footer
            CreateLine(sheet, block.Lines, thickness, startX, rowsEnd, width - padding, rowsEnd, stroke);

            // frame border
            CreateLine(sheet, block.Lines, thickness, startX, startY, width - padding, startY, stroke);
            CreateLine(sheet, block.Lines, thickness, startX, height - padding, width - padding, height - padding, stroke);
            CreateLine(sheet, block.Lines, thickness, startX, startY, startX, height - padding, stroke);
            CreateLine(sheet, block.Lines, thickness, width - padding, startY, width - padding, height - padding, stroke);
        }

        public static void CreateGrid(ISheet<FrameworkElement> sheet, Block block, double startX, double startY, double width, double height, double size, double thickness, Brush stroke)
        {
            for (double y = startY + size; y < height + startY; y += size)
            {
                CreateLine(sheet, block.Lines, thickness, startX, y, width + startX, y, stroke);
            }

            for (double x = startX + size; x < startX + width; x += size)
            {
                CreateLine(sheet, block.Lines, thickness, x, startY, x, height + startY, stroke);
            }
        }

        public static Rectangle CreateSelectionRectangle(double thickness, double x, double y, double width, double height)
        {
            var rect = new Rectangle()
            {
                Fill = new SolidColorBrush(Color.FromArgb(0x3A, 0x00, 0x00, 0xFF)),
                Stroke = new SolidColorBrush(Color.FromArgb(0x7F, 0x00, 0x00, 0xFF)),
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = width,
                Height = height
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);

            return rect;
        }

        #endregion
    }

    #endregion
}
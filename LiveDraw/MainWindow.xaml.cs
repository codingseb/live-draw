﻿using AntFu7.LiveDraw.Helpers;
using AntFu7.LiveDraw.Model;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Bitmap = System.Drawing.Bitmap;
using Brush = System.Windows.Media.Brush;
using CopyPixelOperation = System.Drawing.CopyPixelOperation;
using Graphics = System.Drawing.Graphics;
using Point = System.Windows.Point;
using Screen = System.Windows.Forms.Screen;

namespace AntFu7.LiveDraw
{
    public partial class MainWindow : Window
    {
        public enum EraseMode
        {
            NONE = 0,
            ERASER = 1,
            ERASERBYPOINT = 2
        }

        public EraseMode EraseByPoint_Flag;

        private static Mutex mutex = new Mutex(true, "LiveDraw");
        private static readonly Duration Duration1 = (Duration)Application.Current.Resources["Duration1"];
        private static readonly Duration Duration2 = (Duration)Application.Current.Resources["Duration2"];
        private static readonly Duration Duration3 = (Duration)Application.Current.Resources["Duration3"];
        private static readonly Duration Duration4 = (Duration)Application.Current.Resources["Duration4"];
        private static readonly Duration Duration5 = (Duration)Application.Current.Resources["Duration5"];
        private static readonly Duration Duration7 = (Duration)Application.Current.Resources["Duration7"];
        private static readonly Duration Duration10 = (Duration)Application.Current.Resources["Duration10"];

        #region /---------Lifetime---------/

        public MainWindow()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                _history = new Stack<StrokesHistoryNode>();
                _redoHistory = new Stack<StrokesHistoryNode>();
                if (!Directory.Exists("Save"))
                    Directory.CreateDirectory("Save");

                InitializeComponent();
                SetOrientation(Persistence.Instance.Orientation);
                InitPositioning();
                SetColor(Persistence.Instance.Color);
                SetEnable(true);
                SetDetailPanel(true);
                SetBrushSize();
                if (Persistence.Instance.LineMode)
                    LineMode(true);

                DetailPanel.Opacity = 0;

                MainInkCanvas.Strokes.StrokesChanged += StrokesChanged;
                MainInkCanvas.MouseLeftButtonDown += StartLine;
                MainInkCanvas.MouseLeftButtonUp += EndLine;
                MainInkCanvas.MouseMove += MakeLine;
                MainInkCanvas.MouseWheel += BrushSize;

                List<ColorPicker> colorPickers = ColorPickersPanel
                        .FindAllVisualDescendants<ColorPicker>()
                        .ToList();

                for (int i = 1; i <= 12; i++)
                {
                    colorPickers[i - 1].ToolTip = $"[F{i}]";
                }
            }
            else
            {
                Application.Current.Shutdown(0);
            }
        }

        private static Screen GetCurrentScreen()
        {
            Win32Point mousePos = new Win32Point();

            Win32.GetCursorPos(ref mousePos);

            return Screen.AllScreens.First(s => s.Bounds.Contains(mousePos.X, mousePos.Y));
        }

        private void InitPositioning()
        {
            Screen currentScreen = GetCurrentScreen();

            if (Persistence.Instance.MultiScreen)
            {
                Left = Screen.AllScreens.Min(s => s.Bounds.Left);
                Top = Screen.AllScreens.Min(s => s.Bounds.Top);
                Width = Screen.AllScreens.Max(s => s.Bounds.Right) - Screen.AllScreens.Min(s => s.Bounds.Left);
                Height = Screen.AllScreens.Max(s => s.Bounds.Bottom) - Screen.AllScreens.Min(s => s.Bounds.Top);

                Canvas.SetLeft(Palette, Math.Abs(Left - currentScreen.Bounds.Left) + Persistence.Instance.PaletteX);
                Canvas.SetTop(Palette, Math.Abs(Top - currentScreen.Bounds.Top) + Persistence.Instance.PaletteY);
            }
            else
            {
                Left = currentScreen.Bounds.Left;
                Top = currentScreen.Bounds.Top;
                Width = currentScreen.Bounds.Width;
                Height = currentScreen.Bounds.Height;

                Canvas.SetLeft(Palette, Persistence.Instance.PaletteX);
                Canvas.SetTop(Palette, Persistence.Instance.PaletteY);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            SaveAndExit();
        }

        private void SaveAndExit()
        {
            if (IsUnsaved())
                QuickSave("ExitingAutoSave_");

            Application.Current.Shutdown(0);
        }

        #endregion

        #region /---------Judge--------/

        private bool _saved;

        private bool IsUnsaved()
        {
            return MainInkCanvas.Strokes.Count != 0 && !_saved;
        }

        private bool PromptToSave()
        {
            if (!IsUnsaved())
                return true;
            var r = MessageBox.Show("You have unsaved work, do you want to save it now?", "Unsaved data",
                MessageBoxButton.YesNoCancel);
            if (r == MessageBoxResult.Yes || r == MessageBoxResult.OK)
            {
                QuickSave();
                return true;
            }
            return r == MessageBoxResult.No || r == MessageBoxResult.None;
        }

        #endregion

        #region /---------Setter---------/

        private ColorPicker _selectedColor;
        private bool _inkVisibility = true;
        private bool _displayDetailPanel;
        private bool _eraserMode;
        private bool _enable;
        private readonly int[] _brushSizes = { 1, 3, 5, 8, 13, 20, 30 };
        private int _brushIndex = Persistence.Instance.BrushIndex;

        private void SetDetailPanel(bool v)
        {
            if (v)
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(180, Duration5));
                ColorPickersPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration4));
                DetailPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration4));
                PaletteGrip.BeginAnimation(WidthProperty, new DoubleAnimation(130, Duration3));
                MinimizeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration3));
                MinimizeButton.BeginAnimation(HeightProperty, new DoubleAnimation(0, 25, Duration3));
                MinimizeButton.BeginAnimation(WidthProperty, new DoubleAnimation(0, 25, Duration3));
            }
            else
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, Duration5));
                ColorPickersPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration4));
                DetailPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration4));
                PaletteGrip.BeginAnimation(WidthProperty, new DoubleAnimation(80, Duration3));
                MinimizeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration3));
                MinimizeButton.BeginAnimation(HeightProperty, new DoubleAnimation(25, 0, Duration3));
                MinimizeButton.BeginAnimation(WidthProperty, new DoubleAnimation(25, 0, Duration3));
            }
            _displayDetailPanel = v;
        }

        private void SetInkVisibility(bool v)
        {
            MainInkCanvas.BeginAnimation(OpacityProperty,
                v ? new DoubleAnimation(0, 1, Duration3) : new DoubleAnimation(1, 0, Duration3));
            HideButton.IsActived = !v;
            if (ColorSelectionPanel.Visibility == Visibility.Collapsed)
                SetEnable(v);
            _inkVisibility = v;
        }

        private void SetEnable(bool b)
        {
            DisableColorSelection();
            EnableButton.IsActived = !b;
            Background = Application.Current.Resources[b ? "FakeTransparent" : "TrueTransparent"] as Brush;
            _enable = b;
            MainInkCanvas.UseCustomCursor = false;

            //SetTopMost(false);
            if (_enable)
            {
                LineButton.IsActived = false;
                EraserButton.IsActived = false;
                SetStaticInfo("LiveDraw");
                MainInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
            else
            {
                SetStaticInfo("Locked");
                MainInkCanvas.EditingMode = InkCanvasEditingMode.None; //No inking possible
            }
        }

        private void SetColor(string color)
        {
            try
            {
                color = ((Color)ColorConverter.ConvertFromString(color)).ToString();

                ColorPicker colorPicker = ColorPickersPanel.FindVisualDescendant<ColorPicker>(cp => cp.Background.ToString().Equals(color));

                if (colorPicker != null)
                {
                    SetColor(colorPicker);
                    return;
                }
            }
            catch { }

            SetColor("Black");
        }

        private void SetColor(ColorPicker colorPicker)
        {
            if (colorPicker.Background is not SolidColorBrush solidColorBrush) return;

            Persistence.Instance.Color = solidColorBrush.ToString();

            var ani = new ColorAnimation(solidColorBrush.Color, Duration3);

            MainInkCanvas.DefaultDrawingAttributes.Color = solidColorBrush.Color;
            brushPreview.Background.BeginAnimation(SolidColorBrush.ColorProperty, ani);
            if (_selectedColor != null)
                _selectedColor.IsActived = false;
            colorPicker.IsActived = true;
            _selectedColor = colorPicker;
        }

        private void SetBrushSize()
        {
            if (_brushIndex > _brushSizes.Length - 1)
                _brushIndex = 0;
            else if (_brushIndex < 0)
                _brushIndex = _brushSizes.Length - 1;

            Persistence.Instance.BrushIndex = _brushIndex;

            double size = _brushSizes[_brushIndex];

            if (MainInkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                MainInkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                MainInkCanvas.EraserShape = new EllipseStylusShape(size, size);
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
            else
            {
                MainInkCanvas.DefaultDrawingAttributes.Height = size;
                MainInkCanvas.DefaultDrawingAttributes.Width = size;
                brushPreview?.BeginAnimation(HeightProperty, new DoubleAnimation(size, Duration4));
                brushPreview?.BeginAnimation(WidthProperty, new DoubleAnimation(size, Duration4));
            }
        }

        private void SetEraserMode(bool v)
        {
            DisableColorSelection();
            EraserButton.IsActived = v;
            _eraserMode = v;
            MainInkCanvas.UseCustomCursor = false;

            if (_eraserMode)
            {
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                SetStaticInfo("Eraser Mode");
            }
            else
            {
                SetEnable(_enable);
            }
        }

        private void SetOrientation(bool v)
        {
            PaletteRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(v ? -90 : 0, Duration4));
            Palette.BeginAnimation(MinWidthProperty, new DoubleAnimation(v ? 90 : 0, Duration7));
        }

        #endregion

        #region /---------IO---------/

        private StrokeCollection _preLoadStrokes = null;
        private void QuickSave(string filename = "QuickSave_")
        {
            Save(new FileStream("Save\\" + filename + GenerateFileName(), FileMode.OpenOrCreate));
        }

        private void Save(Stream fs)
        {
            try
            {
                if (fs == Stream.Null) return;
                MainInkCanvas.Strokes.Save(fs);
                _saved = true;
                Display("Ink saved");
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Fail to save");
            }
        }

        private StrokeCollection Load(Stream fs)
        {
            try
            {
                return new StrokeCollection(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Fail to load");
            }
            return new StrokeCollection();
        }

        private void AnimatedReload(StrokeCollection sc)
        {
            _preLoadStrokes = sc;
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += LoadAniCompleted;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }

        private void LoadAniCompleted(object sender, EventArgs e)
        {
            if (_preLoadStrokes == null) return;
            MainInkCanvas.Strokes = _preLoadStrokes;
            Display("Ink loaded");
            _saved = true;
            ClearHistory();
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }

        #endregion

        #region /---------Generator---------/
        private static string GenerateFileName(string fileExt = ".fdw")
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss") + fileExt;
        }

        #endregion

        #region /---------Helper---------/

        private string _staticInfo = "";
        private bool _displayingInfo;

        private async void Display(string info)
        {
            InfoBox.Text = info;
            _displayingInfo = true;
            await InfoDisplayTimeUp(new Progress<string>(box => InfoBox.Text = box));
        }

        private Task InfoDisplayTimeUp(IProgress<string> box)
        {
            return Task.Run(() =>
            {
                Task.Delay(2000).Wait();
                box.Report(_staticInfo);
                _displayingInfo = false;
            });
        }

        private void SetStaticInfo(string info)
        {
            _staticInfo = info;
            if (!_displayingInfo)
                InfoBox.Text = _staticInfo;
        }

        private static Stream SaveDialog(string initFileName, string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
                FileName = initFileName,
                InitialDirectory = Directory.GetCurrentDirectory() + "Save"
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }

        private static Stream OpenDialog(string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }

        void EraserFunction()
        {
            LineMode(false);
            if (EraseByPoint_Flag == EraseMode.NONE)
            {
                SetEraserMode(!_eraserMode);
                EraserButton.ToolTip = "Toggle eraser (by point) mode (D)";
                EraseByPoint_Flag = EraseMode.ERASER;
            }
            else if (EraseByPoint_Flag == EraseMode.ERASER)
            {
                EraserButton.IsActived = true;
                SetStaticInfo("Eraser Mode (Point)");
                EraserButton.ToolTip = "Toggle eraser - OFF";
                double s = MainInkCanvas.EraserShape.Height;
                MainInkCanvas.EraserShape = new EllipseStylusShape(s, s);
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                EraseByPoint_Flag = EraseMode.ERASERBYPOINT;
            }
            else if (EraseByPoint_Flag == EraseMode.ERASERBYPOINT)
            {
                SetEraserMode(!_eraserMode);
                EraserButton.ToolTip = "Toggle eraser mode (E)";
                EraseByPoint_Flag = EraseMode.NONE;
            }
        }

        #endregion

        #region /---------Ink---------/

        private readonly Stack<StrokesHistoryNode> _history;
        private readonly Stack<StrokesHistoryNode> _redoHistory;
        private bool _ignoreStrokesChange;

        private void Undo()
        {
            if (!CanUndo()) return;
            var last = Pop(_history);
            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Added)
                MainInkCanvas.Strokes.Remove(last.Strokes);
            else
                MainInkCanvas.Strokes.Add(last.Strokes);
            _ignoreStrokesChange = false;
            Push(_redoHistory, last);
        }

        private void Redo()
        {
            if (!CanRedo()) return;
            var last = Pop(_redoHistory);
            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Removed)
                MainInkCanvas.Strokes.Remove(last.Strokes);
            else
                MainInkCanvas.Strokes.Add(last.Strokes);
            _ignoreStrokesChange = false;
            Push(_history, last);
        }

        private static void Push(Stack<StrokesHistoryNode> collection, StrokesHistoryNode node)
        {
            collection.Push(node);
        }

        private static StrokesHistoryNode Pop(Stack<StrokesHistoryNode> collection)
        {
            return collection.Count == 0 ? null : collection.Pop();
        }

        private bool CanUndo()
        {
            return _history.Count != 0;
        }

        private bool CanRedo()
        {
            return _redoHistory.Count != 0;
        }

        private void StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_ignoreStrokesChange) return;
            _saved = false;
            if (e.Added.Count != 0)
                Push(_history, new StrokesHistoryNode(e.Added, StrokesHistoryNodeType.Added));
            if (e.Removed.Count != 0)
                Push(_history, new StrokesHistoryNode(e.Removed, StrokesHistoryNodeType.Removed));
            ClearHistory(_redoHistory);
        }

        private void ClearHistory()
        {
            ClearHistory(_history);
            ClearHistory(_redoHistory);
        }

        private static void ClearHistory(Stack<StrokesHistoryNode> collection)
        {
            collection?.Clear();
        }

        private void Clear()
        {
            MainInkCanvas.Strokes.Clear();
            ClearHistory();
        }

        private void AnimatedClear()
        {
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += ClearAniComplete;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }

        private void ClearAniComplete(object sender, EventArgs e)
        {
            Clear();
            Display("Cleared");
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }

        #endregion

        #region /---------UI---------/

        private void DetailToggler_Click(object sender, RoutedEventArgs e)
        {
            SetDetailPanel(!_displayDetailPanel);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = false;
            var anim = new DoubleAnimation(0, Duration3);
            anim.Completed += Exit;
            BeginAnimation(OpacityProperty, anim);
        }

        private void ColorPickers_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ColorPicker colorPicker) return;
            SetColor(colorPicker);

            if (EraseByPoint_Flag != EraseMode.NONE)
            {
                SetEraserMode(false);
                EraseByPoint_Flag = EraseMode.NONE;
                EraserButton.ToolTip = "Toggle eraser mode (E)";
            }
        }

        private void BrushSize(object sender, MouseWheelEventArgs e)
        {
            int delta = e.Delta;
            if (delta < 0)
                _brushIndex--;
            else
                _brushIndex++;

            SetBrushSize();
        }

        private void BrushSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            _brushIndex++;
            SetBrushSize();
        }

        private void BrushSwitchButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _brushIndex--;
            SetBrushSize();
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            LineMode(!Persistence.Instance.LineMode);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_enable)
            {
                EraserFunction();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            AnimatedClear(); //Warning! to missclick erasermode (confirmation click?)
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Persistence.Instance.Topmost = !Persistence.Instance.Topmost;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            QuickSave();
        }

        private void SaveButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            Save(SaveDialog(GenerateFileName()));
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PromptToSave()) return;
            var s = OpenDialog();
            if (s == Stream.Null) return;
            AnimatedReload(Load(s));
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportInk();
        }

        private void ExportInk()
        {
            if (Persistence.Instance.MultiScreen && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                ExportScreenInk(GetCurrentScreen());
            }
            else
            {
                ExportAllScreensInk();
            }
        }

        private void ExportScreenInk(System.Windows.Forms.Screen screen)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                var fromHwnd = Graphics.FromHwnd(IntPtr.Zero);
                var x = Math.Abs(Screen.AllScreens.Min(s => s.Bounds.Left) - screen.Bounds.Left);
                var y = Math.Abs(Screen.AllScreens.Min(s => s.Bounds.Top) - screen.Bounds.Top);

                var w = (int)(screen.Bounds.Width * fromHwnd.DpiX / 96.0);
                var h = (int)(screen.Bounds.Height * fromHwnd.DpiY / 96.0);

                var s = SaveDialog("ImageExport_" + GenerateFileName(".png"), ".png",
                    "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                var rtb = new RenderTargetBitmap((int)MainInkCanvas.ActualWidth, (int)MainInkCanvas.ActualHeight, 96d,
                    96d, PixelFormats.Pbgra32);
                rtb.Render(MainInkCanvas);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                CroppedBitmap crop = new CroppedBitmap(rtb, new Int32Rect { X = x, Y = y, Width = w, Height = h });
                BitmapEncoder cropEncoder = new PngBitmapEncoder();
                cropEncoder.Frames.Add(BitmapFrame.Create(crop));

                cropEncoder.Save(s);

                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
        }

        private void ExportAllScreensInk()
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                var s = SaveDialog("ImageExport_" + GenerateFileName(".png"), ".png",
                    "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                var rtb = new RenderTargetBitmap((int)MainInkCanvas.ActualWidth, (int)MainInkCanvas.ActualHeight, 96d,
                    96d, PixelFormats.Pbgra32);
                rtb.Render(MainInkCanvas);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(s);

                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
        }

        private delegate void NoArgDelegate();
        private void ExportButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            ExportScreen();
        }

        private void ExportScreen()
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                ExportFullAllScreen();
            }
            else
            {
                ExportFullScreen(GetCurrentScreen());
            }
        }

        private void ExportFullScreen(System.Windows.Forms.Screen screen)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                Palette.Opacity = 0;
                var s = SaveDialog("ImageExportWithBackground_" + GenerateFileName(".png"), ".png", "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                Palette.Dispatcher.Invoke(DispatcherPriority.Render, (NoArgDelegate)delegate { });
                Thread.Sleep(100);
                var fromHwnd = Graphics.FromHwnd(IntPtr.Zero);
                var w = (int)(screen.Bounds.Width * fromHwnd.DpiX / 96.0);
                var h = (int)(screen.Bounds.Height * fromHwnd.DpiY / 96.0);
                var image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics.FromImage(image).CopyFromScreen(screen.Bounds.Left, screen.Bounds.Top, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
                image.Save(s, ImageFormat.Png);
                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
            finally
            {
                Palette.Opacity = 1;
            }
        }

        private void ExportFullAllScreen()
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                Palette.Opacity = 0;
                var s = SaveDialog("ImageExportWithBackground_" + GenerateFileName(".png"), ".png", "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                Palette.Dispatcher.Invoke(DispatcherPriority.Render, (NoArgDelegate)delegate { });
                Thread.Sleep(100);
                var fromHwnd = Graphics.FromHwnd(IntPtr.Zero);
                var w = (int)(Width * fromHwnd.DpiX / 96.0);
                var h = (int)(Height * fromHwnd.DpiY / 96.0);
                var image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics.FromImage(image).CopyFromScreen((int)Left, (int)Top, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
                image.Save(s, ImageFormat.Png);
                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
            finally
            {
                Palette.Opacity = 1;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            SetInkVisibility(!_inkVisibility);
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(!_enable);
            if (_eraserMode)
            {
                SetEraserMode(!_eraserMode);
                EraserButton.ToolTip = "Toggle eraser mode (E)";
                EraseByPoint_Flag = EraseMode.NONE;
            }
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            Persistence.Instance.Orientation = !Persistence.Instance.Orientation;
            SetOrientation(Persistence.Instance.Orientation);
        }

        #endregion

        #region  /---------Docking---------/

        enum DockingDirection
        {
            None,
            Top,
            Left,
            Right
        }

        private int _dockingEdgeThreshold = 30;
        private int _dockingAwaitTime = 10000;
        private int _dockingSideIndent = 290;
        private void AnimatedCanvasMoving(UIElement ctr, Point to, Duration dur)
        {
            ctr.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(ctr), to.Y, dur));
            ctr.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(ctr), to.X, dur));
        }

        private DockingDirection CheckDocking()
        {
            var left = Canvas.GetLeft(Palette);
            var right = Canvas.GetRight(Palette);
            var top = Canvas.GetTop(Palette);

            if (left > 0 && left < _dockingEdgeThreshold)
                return DockingDirection.Left;
            if (right > 0 && right < _dockingEdgeThreshold)
                return DockingDirection.Right;
            if (top > 0 && top < _dockingEdgeThreshold)
                return DockingDirection.Top;
            return DockingDirection.None;
        }

        private void RightDocking()
        {
            AnimatedCanvasMoving(Palette, new Point(ActualWidth + _dockingSideIndent, Canvas.GetTop(Palette)), Duration5);
        }

        private void LeftDocking()
        {
            AnimatedCanvasMoving(Palette, new Point(0 - _dockingSideIndent, Canvas.GetTop(Palette)), Duration5);
        }

        private void TopDocking()
        {
        }

        private async void AwaitDocking()
        {
            await Docking();
        }

        private Task Docking()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(_dockingAwaitTime);
                var direction = CheckDocking();
                if (direction == DockingDirection.Left) LeftDocking();
                if (direction == DockingDirection.Right) RightDocking();
                if (direction == DockingDirection.Top) TopDocking();
            });
        }

        #endregion

        #region /---------Dragging---------/

        private Point _lastMousePosition;
        private bool _isDraging;
        private bool _tempEnable;

        private void StartDrag()
        {
            _lastMousePosition = Mouse.GetPosition(this);
            _isDraging = true;
            Palette.Background = new SolidColorBrush(Colors.Transparent);
            _tempEnable = _enable;
            SetEnable(true);
        }

        private void EndDrag()
        {
            if (_isDraging)
            {
                SetEnable(_tempEnable);
            }
            _isDraging = false;
            Palette.Background = null;

            if (Persistence.Instance.MultiScreen)
            {
                System.Windows.Forms.Screen currentScreen = GetCurrentScreen();
                Persistence.Instance.PaletteX = (int)Canvas.GetLeft(Palette) - (int)Math.Abs(Left - currentScreen.Bounds.Left);
                Persistence.Instance.PaletteY = (int)Canvas.GetTop(Palette) - (int)Math.Abs(Top - currentScreen.Bounds.Top);
            }
            else
            {
                Persistence.Instance.PaletteX = (int)Canvas.GetLeft(Palette);
                Persistence.Instance.PaletteY = (int)Canvas.GetTop(Palette);
            }
        }

        private void PaletteGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.RightButton == MouseButtonState.Pressed)
            {
                ResetPosition();
            }
            else
            {
                StartDrag();
            }
        }

        private void ResetPosition()
        {
            Persistence.Instance.PaletteX = Globals.DEFAULT_PALETTE_X;
            Persistence.Instance.PaletteY = Globals.DEFAULT_PALETTE_Y;
            InitPositioning();
        }

        private void Palette_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraging) return;
            var currentMousePosition = Mouse.GetPosition(this);
            var offset = currentMousePosition - _lastMousePosition;

            Canvas.SetTop(Palette, Canvas.GetTop(Palette) + offset.Y);
            Canvas.SetLeft(Palette, Canvas.GetLeft(Palette) + offset.X);

            _lastMousePosition = currentMousePosition;
        }

        private void Palette_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EndDrag();
        }

        private void Palette_MouseLeave(object sender, MouseEventArgs e)
        {
            EndDrag();
        }

        #endregion

        #region /--------- Shortcuts --------/

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                SetEnable(!_enable);
            }
            else if (e.Key == Key.H)
            {
                SetInkVisibility(!_inkVisibility);
            }
            else if (e.Key == Key.P && e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ResetPosition();
                return;
            }
            else if (e.Key == Key.Escape)
            {
                SaveAndExit();
            }

            if (!_enable)
                return;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key >= Key.F1 && key <= Key.F12 && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                try
                {
                    SetColor(ColorPickersPanel
                        .FindAllVisualDescendants<ColorPicker>()
                        .ToList()
                        .ElementAt(int.Parse(key.ToString().Replace("F", "")) - 1));

                    e.Handled = true;
                }
                catch { }
            }

            switch (e.Key)
            {
                case Key.Z:
                    Undo();
                    break;
                case Key.Y:
                    Redo();
                    break;
                case Key.P:
                    ColorSelection();
                    break;
                case Key.E:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        ExportInk();
                    else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                        ExportFullScreen(GetCurrentScreen());
                    else
                        EraserFunction();
                    break;
                case Key.B:
                    if (_eraserMode)
                        SetEraserMode(false);
                    SetEnable(true);
                    break;
                case Key.L:
                    LineMode(!Persistence.Instance.LineMode);
                    break;
                case Key.Delete:
                    AnimatedClear();
                    break;
                case Key.Add:
                    _brushIndex++;
                    SetBrushSize();
                    break;
                case Key.Subtract:
                    _brushIndex--;
                    SetBrushSize();
                    break;
            }
        }

        #endregion

        #region /------ Line Mode -------/

        private bool _isMoving = false;
        private Point _startPoint;
        private Stroke _lastStroke;

        private void LineMode(bool lineMode)
        {
            if (_enable)
            {
                DisableColorSelection();
                Persistence.Instance.LineMode = lineMode;
                if (lineMode)
                {
                    EraseByPoint_Flag = EraseMode.NONE;
                    MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    SetEraserMode(false);
                    EraserButton.IsActived = false;
                    LineButton.IsActived = lineMode;
                    SetStaticInfo("LineMode");
                    MainInkCanvas.EditingMode = InkCanvasEditingMode.None;
                    MainInkCanvas.UseCustomCursor = true;
                }
                else
                {
                    SetEnable(true);
                }
            }
        }

        private void StartLine(object sender, MouseButtonEventArgs e)
        {
            _isMoving = true;
            _startPoint = e.GetPosition(MainInkCanvas);
            _lastStroke = null;
            _ignoreStrokesChange = true;
        }

        private void EndLine(object sender, MouseButtonEventArgs e)
        {
            if (_isMoving)
            {
                if (_lastStroke != null)
                {
                    StrokeCollection collection = new StrokeCollection
                    {
                        _lastStroke
                    };
                    Push(_history, new StrokesHistoryNode(collection, StrokesHistoryNodeType.Added));
                }
            }
            _isMoving = false;
            _ignoreStrokesChange = false;
        }

        private void MakeLine(object sender, MouseEventArgs e)
        {
            if (!_isMoving)
                return;

            DrawingAttributes newLine = MainInkCanvas.DefaultDrawingAttributes.Clone();
            newLine.StylusTip = StylusTip.Ellipse;
            newLine.IgnorePressure = true;

            Point _endPoint = e.GetPosition(MainInkCanvas);

            List<Point> pList = new List<Point>
            {
                new Point(_startPoint.X, _startPoint.Y),
                new Point(_endPoint.X, _endPoint.Y),
            };

            StylusPointCollection point = new StylusPointCollection(pList);
            Stroke stroke = new Stroke(point) { DrawingAttributes = newLine, };

            if (_lastStroke != null)
                MainInkCanvas.Strokes.Remove(_lastStroke);
            if (stroke != null)
                MainInkCanvas.Strokes.Add(stroke);

            _lastStroke = stroke;
        }

        #endregion

        #region /------ MultiScreen ------/

        private void ScreenSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            Persistence.Instance.MultiScreen = !Persistence.Instance.MultiScreen;
            //Clear();
            InitPositioning();
        }

        #endregion

        #region /------ Color Selection ------/

        private void ColorSelector_Click(object sender, RoutedEventArgs e)
        {
            ColorSelection();
        }

        private void ColorSelectionPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Win32Point point = new Win32Point();
                if (Win32.GetCursorPos(ref point))
                {
                    ColorSelectionPicker.Background = new SolidColorBrush(Win32.GetPixelColor(point.X, point.Y));
                    SetColor(ColorSelectionPicker);
                }
            }
            catch { }
            finally
            {
                DisableColorSelection();
            }
        }

        private void ColorSelection()
        {
            if (ColorSelectionPanel.Visibility == Visibility.Collapsed)
            {
                SetColor(ColorSelectionPicker);
                ColorSelectionPanel.Visibility = Visibility.Visible;
                ColorSelectorButton.IsActived = true;
            }
            else
            {
                DisableColorSelection();
            }
        }

        private void DisableColorSelection()
        {
            ColorSelectionPanel.Visibility = Visibility.Collapsed;
            ColorSelectorButton.IsActived = false;
        }

        private void ColorSelectionPicker_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ColorSelectionPicker.Background = Brushes.White;
            SetColor(ColorSelectionPicker);
        }

        #endregion
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;

namespace SrcChess2 {
    /// <summary>
    /// Toolbar for the Chess Program
    /// </summary>
    public partial class ChessToolBar {
        public ChessToolBar() {
            InitializeComponent();
            ProgressBar.Visibility = Visibility.Hidden;
        }

        public void StartProgressBar() {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Start();
        }

        public void EndProgressBar() {
            ProgressBar.Stop();
            ProgressBar.Visibility = Visibility.Hidden;
        }
    }

    public class ToolBarButton : Button {
        public static readonly DependencyProperty ImageProperty;
        public static readonly DependencyProperty DisabledImageProperty;
        public static readonly DependencyProperty FlipProperty;
        public static readonly DependencyProperty TextProperty;
        public static readonly DependencyProperty DisplayStyleProperty;
        private Image?                            m_imageCtrl;
        private TextBlock?                        m_textCtrl;

        public enum TbDisplayStyle {
            Image,
            Text,
            ImageAndText
        }
        static ToolBarButton() {
            ImageProperty         = DependencyProperty.Register("Image",
                                                                typeof(ImageSource),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: null,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              ImageChanged));
            DisabledImageProperty = DependencyProperty.Register("DisabledImage",
                                                                typeof(ImageSource),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: null,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              DisabledImageChanged));
            FlipProperty          = DependencyProperty.Register("Flip",
                                                                typeof(bool),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: false,
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                                              FlipChanged));
            TextProperty          = DependencyProperty.Register("Text",
                                                                typeof(string),
                                                                typeof(ToolBarButton),
                                                                new FrameworkPropertyMetadata(defaultValue: "",
                                                                                              FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                              TextChanged));
            DisplayStyleProperty  = DependencyProperty.RegisterAttached("DisplayStyle",
                                                                       typeof(TbDisplayStyle),
                                                                       typeof(ToolBarButton),
                                                                       new FrameworkPropertyMetadata(defaultValue: TbDisplayStyle.Text,
                                                                                                     FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                                                                     DisplayStyleChanged));
            IsEnabledProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(defaultValue: true, new PropertyChangedCallback(IsEnabledChanged)));
        }

        public ToolBarButton() : base() {
            Style = new Style(typeof(ToolBarButton), (Style)FindResource(ToolBar.ButtonStyleKey));
            BuildInnerButton();
        }

        private static void ImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Image displayed in button")]
        public ImageSource Image {
            get => (ImageSource)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }


        private static void DisabledImageChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Disabled Image displayed in button")]
        public ImageSource DisabledImage {
            get => (ImageSource)GetValue(DisabledImageProperty);
            set => SetValue(DisabledImageProperty, value);
        }

        private static void FlipChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Flip horizontally the Image displayed in button")]
        public bool Flip {
            get => (bool)GetValue(FlipProperty);
            set => SetValue(FlipProperty, value);
        }

        private static void TextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Text displayed in button")]
        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        private static void DisplayStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton tbItem && e.OldValue != e.NewValue) {
                tbItem.UpdateInnerButton();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Layout")]
        [Description("Display Style applied to the button")]
        public TbDisplayStyle DisplayStyle {
            get => (TbDisplayStyle)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        public static void SetDisplayStyle(DependencyObject element, TbDisplayStyle displayStyle) {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(DisplayStyleProperty, displayStyle);
        }


        public static TbDisplayStyle GetDisplayStyle(DependencyObject element)
            => element == null ? throw new ArgumentNullException(nameof(element)) : (TbDisplayStyle)element.GetValue(DisplayStyleProperty);

        private new static void IsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ToolBarButton me && e.OldValue != e.NewValue) {
                me.UpdateInnerButton();
            }
        }

        private void SetImage(bool bFlip) {
            ScaleTransform  scaleTransform;

            m_imageCtrl!.Source      = (IsEnabled) ? Image : DisabledImage;
            m_imageCtrl.OpacityMask = null;
            if (bFlip) {
                m_imageCtrl.RenderTransformOrigin = new Point(0.5, 0.5);
                scaleTransform = new ScaleTransform {
                    ScaleX = -1
                };
                m_imageCtrl.RenderTransform = scaleTransform;
            }
        }

        private void BuildInnerButton() {
            Grid grid;

            grid = new Grid {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            m_imageCtrl = new Image();
            m_textCtrl  = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = System.Windows.VerticalAlignment.Center };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(m_imageCtrl, 0);
            grid.Children.Add(m_imageCtrl);
            Grid.SetColumn(m_textCtrl, 1);
            grid.Children.Add(m_textCtrl);
            Content = grid;
        }

        private void UpdateInnerButton() {
            TbDisplayStyle displayStyle;
            Grid           grid;
            string         strText;

            grid         = (Grid)Content;
            displayStyle = DisplayStyle;
            strText      = Text;
            if (Image != null && (displayStyle == TbDisplayStyle.Image || displayStyle == TbDisplayStyle.ImageAndText)) {
                grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                SetImage(Flip);
            } else {
                m_imageCtrl!.Source             = null;
                grid.ColumnDefinitions[0].Width = new GridLength(0);
            }
            if (!string.IsNullOrEmpty(strText) && (displayStyle == TbDisplayStyle.Text || displayStyle == TbDisplayStyle.ImageAndText)) {
                grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                m_textCtrl!.Text                = strText;
            } else {
                m_textCtrl!.Text                = string.Empty;
                grid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        } 
    }
}

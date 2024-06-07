using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SrcChess2 {
    public partial class CustomColorPicker : UserControl{
        private Color   m_selectedColor      = Colors.Transparent;
        private bool    m_isContexMenuOpened = false;

        public CustomColorPicker() {
            InitializeComponent();
            b.ContextMenu.Closed       += new RoutedEventHandler(ContextMenu_Closed);
            b.ContextMenu.Opened       += new RoutedEventHandler(ContextMenu_Opened);
            b.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(B_PreviewMouseLeftButtonUp);
        }

        public event Action<Color>? SelectedColorChanged;

        public string HexValue { get; set; } = "";


        public Color SelectedColor{
            get => m_selectedColor;
            set {
                if (m_selectedColor != value) {
                    m_selectedColor = value;
                    cp.CustomColor  = value;
                    Update();
                }
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => m_isContexMenuOpened = true;
        private void Update() {
            recContent.Fill = new SolidColorBrush(cp.CustomColor);
            HexValue        = string.Format("#{0}", cp.CustomColor.ToString()[1..]);
            m_selectedColor = cp.CustomColor;
        }
        private void ContextMenu_Closed(object sender, RoutedEventArgs e) {
            if (!b.ContextMenu.IsOpen) {
                SelectedColorChanged?.Invoke(cp.CustomColor);
                Update();
            }
            m_isContexMenuOpened = false;
        }
        private void B_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!m_isContexMenuOpened) {
                if (b.ContextMenu != null && b.ContextMenu.IsOpen == false) {
                    b.ContextMenu.PlacementTarget   = b;
                    b.ContextMenu.Placement         = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ContextMenuService.SetPlacement(b, System.Windows.Controls.Primitives.PlacementMode.Bottom);
                    b.ContextMenu.IsOpen            = true;
                }
            }
        }
    }
}

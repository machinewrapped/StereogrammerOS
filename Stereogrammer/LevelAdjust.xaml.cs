// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Stereogrammer.Model;

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for LevelAdjust.xaml
    /// </summary>
    public partial class LevelAdjust : Window
    {
        Depthmap preview = null;
        public LevelAdjustments adjustments;

        public LevelAdjust( Depthmap src )
        {
            InitializeComponent();
            preview = new Depthmap( src.GetToScale( (int)imagePreview.Width, (int)imagePreview.Height ) );
        }

        private void slider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if ( preview != null )
            {
                adjustments = new LevelAdjustments( blackin.Value, whitein.Value, blackout.Value, whiteout.Value, gamma.Value, (bool)bHardBlacks.IsChecked );
                imagePreview.Source = preview.GetLevelAdjusted( adjustments );
            }
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            if ( preview != null )
            {
                adjustments = new LevelAdjustments( blackin.Value, whitein.Value, blackout.Value, whiteout.Value, gamma.Value, (bool)bHardBlacks.IsChecked );
                imagePreview.Source = preview.GetLevelAdjusted( adjustments );
            }
        }

        private void bHardBlacks_Checked( object sender, RoutedEventArgs e )
        {
            if ( preview != null )
            {
                adjustments = new LevelAdjustments( blackin.Value, whitein.Value, blackout.Value, whiteout.Value, gamma.Value, (bool)bHardBlacks.IsChecked );
                imagePreview.Source = preview.GetLevelAdjusted( adjustments );
            }
        }

        private void buttonOK_Click( object sender, RoutedEventArgs e )
        {
            adjustments = new LevelAdjustments( blackin.Value, whitein.Value, blackout.Value, whiteout.Value, gamma.Value, (bool)bHardBlacks.IsChecked );
            DialogResult = true;
            Close();
        }
    }
}

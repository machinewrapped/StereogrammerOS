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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

using Stereogrammer.ViewModel;
using Stereogrammer.Model;      // TEMP?

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for PreviewPane.xaml
    /// </summary>
    public partial class PreviewPane : UserControl
    {
        new public double ActualWidth { get { return gridPreview.ActualWidth; } }
        new public double ActualHeight { get { return gridPreview.ActualHeight; } }

        public PreviewPane()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the preview pane to a previewable item
        /// </summary>
        /// <param name="item"></param>
        public void SetPreviewItem( object item )
        {
            if ( item is Thumbnail )
            {
                // Set the underlying object's preview then add a context menu
                Thumbnail thumb = (Thumbnail)item;
                SetPreviewItem( thumb.ThumbnailOf );
            }
            else if ( item is BitmapType )
            {
                BitmapType preview = (BitmapType)item;
                imagePreview.Source = preview.Bitmap;

                InputBindings.Clear();

                MouseBinding doubleclick = new MouseBinding( Commands.CmdFullscreen, new MouseGesture( MouseAction.LeftDoubleClick ) );
                doubleclick.CommandTarget = this;
                doubleclick.CommandParameter = preview;
                InputBindings.Add( doubleclick );

                List<MenuItem> menuitems = GetMenuItems( item, item );
                ContextMenu = new ContextMenu() { ItemsSource = menuitems };
            }
            else
            {
                imagePreview.Source = null;
                InputBindings.Clear();
                ContextMenu = null;
            }
        }

        /// <summary>
        /// Get list of menu items for commands the object supports
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public List<MenuItem> GetMenuItems( object item, object parameter )
        {
            List<CommandView> commands = Commands.GetSupportedCommands( item );

            if (commands != null)
	        {
                List<MenuItem> items = new List<MenuItem>();
                foreach ( CommandView cmd in commands )
                {
                    items.Add( new MenuItem() { Header = cmd.LongName, Command = cmd.Command, CommandParameter = parameter } );
                }

                return items;		 
	        }

            return null;
        }

        private void UserControl_Unloaded( object sender, RoutedEventArgs e )
        {
        }

        private void UserControl_Loaded( object sender, RoutedEventArgs e )
        {
        }

    }
}

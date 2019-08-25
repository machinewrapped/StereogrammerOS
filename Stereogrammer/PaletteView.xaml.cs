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
using System.Windows.Controls.Primitives;

using System.Reflection;

using Stereogrammer.ViewModel;

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for PaletteView.xaml
    /// </summary>
    public partial class PaletteView : UserControl
    {
        public static readonly DependencyProperty PaletteProperty =
              DependencyProperty.Register( "Palette", typeof( Palette ), typeof( PaletteView ) );

        public static readonly DependencyProperty DefaultDoubleClickProperty =
              DependencyProperty.Register( "DefaultDoubleClick", typeof( RoutedCommand ), typeof( PaletteView ) );

        public List<Button> Buttons { get; set; }

        public Thumbnail SelectedThumbnail { get { return Palette.GetSelectedThumbnail(); } }
        public List<Thumbnail> MultiselectedThumbnails { get { return Palette.GetMultiselectedThumbnails(); } }

        public RoutedCommand DefaultDoubleClick
        {
            get { return (RoutedCommand)GetValue( DefaultDoubleClickProperty ); }
            set { SetValue( DefaultDoubleClickProperty, value ); Palette.DefaultDoubleClick = value; }
        }

        public Palette Palette
        {
            get { return (Palette)GetValue( PaletteProperty ); }
            set { SetValue( PaletteProperty, value ); }
        }

        public PaletteView()
        {
            InitializeComponent();
            Buttons = new List<Button>();
            ButtonsPanel.ItemsSource = Buttons;
            this.CommandBindings.Add( new CommandBinding( Commands.CmdClearPalette, CmdClearPaletteExecuted, CmdClearPaletteCanExecute ) );
            this.CommandBindings.Add( new CommandBinding( Commands.CmdDeleteSelectedItems, CmdDeleteSelectedItemsExecuted, CmdDeleteSelectedItemsCanExecute ) );
            this.CommandBindings.Add( new CommandBinding( Commands.CmdSelectAndAddFiles, CmdSelectAndAddFilesExecuted, CmdSelectAndAddFilesCanExecute ) );
            this.CommandBindings.Add( new CommandBinding( Commands.CmdSelectItem, CmdSelectItemExecuted, CmdSelectItemCanExecute ) );
        }

        /// <summary>
        /// Command to clear a palette... test of command routing I guess
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdClearPaletteExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Palette.Clear();
        }

        public void CmdClearPaletteCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            if ( Palette != null )
            {
                e.CanExecute = ( Palette.NumThumbnails > Palette.NumUnremovable );
            }
            else
            {
                e.CanExecute = false;
            }
        }

        /// <summary>
        /// Delete the selected thumbnails
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdDeleteSelectedItemsExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Palette.RemoveSelectedItems();
        }

        public void CmdDeleteSelectedItemsCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            if ( Palette != null )
            {
                e.CanExecute = ( SelectedThumbnail != null && SelectedThumbnail.CanRemove ) || ( MultiselectedThumbnails.Count > 0 );
            }
            else
            {
                e.CanExecute = false;
            }
        }


        // Command to add files with a directory selector
        public void CmdSelectAndAddFilesExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Image"; // Default file name
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "Image Files|*.jpg;*.bmp;*.gif;*.png"; // Filter files by extension
            dlg.Multiselect = true;
            dlg.InitialDirectory = Palette.sDefaultDirectory;
            dlg.InitialDirectory.Replace( @"\\", @"\" );
            dlg.RestoreDirectory = false;

            Nullable<bool> result = dlg.ShowDialog();

            if ( result == true )
            {
                Palette.sDefaultDirectory = new System.IO.FileInfo( dlg.FileName ).DirectoryName;
                Palette.Bitmaps.Populate( dlg.FileNames.ToArray() );
            }
        }

        public void CmdSelectAndAddFilesCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            if ( Palette != null )
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        /// <summary>
        /// Handle the logic for clicking on a thumbnail... checking the keyboard state directly is probably evil,
        /// but the alternative is 4 different commands and 4 different input bindings... which just seems stupid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdSelectItemExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Thumbnail thumb = (Thumbnail)e.Parameter;
            Palette.SelectionLogic( thumb, Keyboard.Modifiers.HasFlag( ModifierKeys.Shift ), Keyboard.Modifiers.HasFlag( ModifierKeys.Control ) );
        }

        public void CmdSelectItemCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = e.Parameter is Thumbnail;
        }

        private void UserControl_Loaded( object sender, RoutedEventArgs e )
        {
            if ( Palette != null )
            {
                // I know I should be able to do this in the XAML, but fucked if I can make it work... 
                // DataContext is the view model, and Palette property isn't bound until later.  If set data context to the PaletteView, can't find the Palette to bind it to.
                Thumbnails.ItemsSource = Palette.Thumbnails;
                Palette.OnThumbnailSelected += event_ThumbnailSelected;
                event_ThumbnailSelected( Palette, Palette.GetSelectedThumbnail() );
            }
        }

        /// <summary>
        /// Try to update the scroll viewers to keep selected item visible...
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="thumb"></param>
        private void event_ThumbnailSelected( Palette palette, Thumbnail thumb )
        {
            // Disgusting, and doesn't work with a virtualising stack panel because there's no UI element generated if it's not visible!
            FrameworkElement element = Thumbnails.ItemContainerGenerator.ContainerFromItem( thumb ) as FrameworkElement;

            if ( element != null )
            {
                element.BringIntoView();
            }            
        }
        
        /// <summary>
        /// If image fails to load or decode, just ignore it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_ImageFailed( object sender, ExceptionRoutedEventArgs e )
        {
            Image image = (Image)e.Source;      // or sender?
            e.Handled = true;
        }
    }
}

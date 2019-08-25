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

using Stereogrammer.ViewModel;

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for FullscreenView.xaml
    /// </summary>
    public partial class FullscreenView : Window
    {
        object theItem;

        public delegate void CloseDelegate();

        CloseDelegate OnClose;

        public FullscreenView( object item, CloseDelegate OnClose )
        {
            InitializeComponent();

            theItem = item;

            this.CommandBindings.Add( new CommandBinding( Commands.CmdFullscreenClose, CmdFullscreenCloseExecuted, CmdFullscreenCloseCanExecute ) );

            // Close with double-click
            MouseBinding doubleclick = new MouseBinding( Commands.CmdFullscreenClose, new MouseGesture( MouseAction.LeftDoubleClick ) );
            doubleclick.CommandTarget = this;
            InputBindings.Add( doubleclick );
            previewFullScreen.InputBindings.Add( doubleclick );
            previewFullScreen.imagePreview.InputBindings.Add( doubleclick );

            // Close with Escape... want to close on any key really, so might as well use the old-fashioned keydown event
            InputBindings.Add( new KeyBinding( Commands.CmdFullscreenClose, new KeyGesture( Key.Escape ) ) );

            // Handler for closing
            this.OnClose = OnClose;
        }

        public void CmdFullscreenCloseExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Close();
        }

        public void CmdFullscreenCloseCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = true;
        }

        // No key gesture for 'any key', so use the old-fashioned event method
        private void Fullscreen_KeyDown( object sender, KeyEventArgs e )
        {
            Close();
        }

        private void Fullscreen_Loaded( object sender, RoutedEventArgs e )
        {
            previewFullScreen.SetPreviewItem( theItem );
        }

        private void Fullscreen_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( OnClose != null )
            {
                OnClose();
            }
        }
    }
}

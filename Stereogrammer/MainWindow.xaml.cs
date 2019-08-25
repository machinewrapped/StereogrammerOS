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
using System.IO;
using System.ComponentModel;
using Microsoft.Win32;

using Stereogrammer.ViewModel;
using Stereogrammer.Model;

// TODO: Read/Write Alpha Channel
// TODO: Read/Write options in Stereogram metadata (and find out why resolution isn't showing in xnview thumbnails)
// TODO: Logic could be much simplified with a hidden (or visible) thumbnail palette for previews, 
// TODO: Force a texture to wrap smoothly (be symmetrical) by creating a 2x2 grid with flipped versions
// TODO: Extrude depthmap function
// TODO: Depthmap generation from formulae of form z = f(x,y), fractals etc
// TODO: Depthmap generation from 3D scenes
namespace Stereogrammer
{
    using Algorithm = StereogramAlgorithm.Algorithm;
    using Oversample = StereogramAlgorithm.Oversample;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        StereogrammerViewModel theViewModel = new StereogrammerViewModel();

        public Options Options { get { return theViewModel.Options; } set { theViewModel.Options = value; } }

        public static string[] license;

        public MainWindow()
        {
            this.DataContext = theViewModel;

            // Restore application settings before they're data-bound to UI controls
            if ( Properties.Settings.Default != null )
            {
                Algorithm alg = (Algorithm)Properties.Settings.Default.Algorithm;
                if ( alg >= 0 && (int)alg <= Enum.GetValues( typeof( StereogramAlgorithm.Algorithm ) ).Length )
                {
                    Options.algorithm = alg;
                    Options.resolutionX = Properties.Settings.Default.ResolutionX;
                    Options.resolutionY = Properties.Settings.Default.ResolutionY;
                    Options.FieldDepth = Properties.Settings.Default.Depth;
                    Options.separation = Properties.Settings.Default.Separation;
                    Options.oversample = (Oversample)Properties.Settings.Default.Oversample;
                    Options.bRemoveHiddenSurfaces = Properties.Settings.Default.RemoveHiddenSurfaces;
                    Options.bAddConvergenceDots = Properties.Settings.Default.AddConvergenceDots;
                    Options.bPreserveAspectRatio = Properties.Settings.Default.PreserveAspectRatio;
                }
                else
                {
                    Properties.Settings.Default.Reset();
                }
            }

            InitializeComponent();

            // Restore window position & size - really should validate them I guess
            if ( Properties.Settings.Default.LicenseAccepted == true )      // Take defaults for first run
            {
                this.Top = Properties.Settings.Default.Top;
                this.Left = Properties.Settings.Default.Left;
                this.Height = Properties.Settings.Default.Height;
                this.Width = Properties.Settings.Default.Width;                
            }

            // Bind routed commands to the window
            AddCommandBindings();

        }

        PreviewPane _previewPane;
        PreviewPane PreviewPane
        {
            get { return _previewPane; }
            set
            {
                if ( _previewPane != value )
                {
                    _previewPane = value;
                    theViewModel.UpdatePreview( (int)_previewPane.ActualWidth, (int)_previewPane.ActualHeight );
                }
            }
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            try
            {
                // Set the default directories for each resource type
                string basepath = Environment.CurrentDirectory;
                basepath = basepath.Replace( "\\bin\\Debug", "" );
                basepath = basepath.Replace( "\\bin\\Release", "" );

                // Check for the existence of the license file
                FileInfo licensefile = new FileInfo( System.IO.Path.Combine( basepath, "license.txt" ) );

                if ( licensefile.Exists )
                {
                    license = System.IO.File.ReadAllLines( licensefile.FullName );

                    // Pop up the about box on first run... clicking OK constitutes acceptance
                    if ( Properties.Settings.Default.LicenseAccepted == false )
                    {
                        AboutBox box = new AboutBox( license );
                        box.ShowDialog();
                        Properties.Settings.Default.LicenseAccepted = true;
                    }
                }
                else
                {
                    MessageBox.Show( "License not found!" );
                    Close();
                    return;
                }

                // Restore resource directories or set to defaults
                string imagepath = System.IO.Path.Combine( basepath, "Images" );

                if ( Properties.Settings.Default.DepthmapPath != null && Properties.Settings.Default.DepthmapPath.Length > 0 && Directory.Exists( Properties.Settings.Default.DepthmapPath ) )
                {
                    theViewModel.DepthmapPalette.sDefaultDirectory = Properties.Settings.Default.DepthmapPath;
                }
                else
                {
                    theViewModel.DepthmapPalette.sDefaultDirectory = System.IO.Path.Combine( imagepath, "Depthmaps" );
                }

                if ( Properties.Settings.Default.TexturePath != null && Properties.Settings.Default.TexturePath.Length > 0 && Directory.Exists( Properties.Settings.Default.TexturePath ) )
                {
                    theViewModel.TexturePalette.sDefaultDirectory = Properties.Settings.Default.TexturePath;
                }
                else
                {
                    theViewModel.TexturePalette.sDefaultDirectory = System.IO.Path.Combine( imagepath, "Textures" );
                }

                if ( Properties.Settings.Default.SterogramPath != null && Properties.Settings.Default.SterogramPath.Length > 0 && Directory.Exists( Properties.Settings.Default.SterogramPath ) )
                {
                    theViewModel.StereogramPalette.sDefaultDirectory = Properties.Settings.Default.SterogramPath;
                }
                else
                {
                    theViewModel.StereogramPalette.sDefaultDirectory = System.IO.Path.Combine( imagepath, "Stereograms" );
                }

                // Default preview pane is in the main panel
                PreviewPane = previewMainPanel;

                // Register callbacks to handle events in the view model
                theViewModel.OnPaletteSelected += tabControlPalette_PaletteSelected;
                theViewModel.OnPreviewItemChanged += new Action<object>( item => SetPreviewItem( item ) );
                theViewModel.OnErrorMessage += new Action<string>( message => MessageBox.Show( message, "Error" ) );

                // Populate the view model with initial items
                try
                {
                    theViewModel.Populate();
                }
                catch ( Exception ex )
                {
                    MessageBox.Show( ex.Message, "Error populating resources" );
                }

                // Try to restore selected items... will only work if they were saved to file (and the file still exists)
                if ( Properties.Settings.Default.SelectedDepthmap != null && Properties.Settings.Default.SelectedDepthmap.Length > 0 )
                {
                    Thumbnail dm = theViewModel.DepthmapPalette.FindByFilename( Properties.Settings.Default.SelectedDepthmap );
                    if ( dm != null && dm.ThumbnailOf is Depthmap )
                    {
                        theViewModel.DepthmapPalette.SelectItem( dm );
                    }
                }

                if ( Properties.Settings.Default.SelectedTexture != null && Properties.Settings.Default.SelectedTexture.Length > 0 )
                {
                    Thumbnail tx = theViewModel.TexturePalette.FindByFilename( Properties.Settings.Default.SelectedTexture );
                    if ( tx != null && tx.ThumbnailOf is Texture )
                    {
                        theViewModel.TexturePalette.SelectItem( tx );
                    }
                }

                if ( Properties.Settings.Default.SelectedStereogram != null && Properties.Settings.Default.SelectedStereogram.Length > 0 )
                {
                    Thumbnail sg = theViewModel.StereogramPalette.FindByFilename( Properties.Settings.Default.SelectedStereogram );
                    if ( sg != null && sg.ThumbnailOf is Stereogram )
                    {
                        theViewModel.StereogramPalette.SelectItem( sg );
                    }
                }

                // Show the selected depthmap by default
                theViewModel.PreviewItem = theViewModel.SelectedDepthmap;

                // Register the progress bar as a progress reporter
                theViewModel.RegisterProgressReporter( progress => { progressBar.Value = ( progress / 100 ) * progressBar.Maximum; }
                    , () => { progressBar.Visibility = Visibility.Visible; }
                    , () => { progressBar.Visibility = Visibility.Hidden; } );

            }
            catch ( Exception ex )
            {
                MessageBox.Show( String.Format( "Failed to initialise - {0}", ex.Message ) );
                Close();
            }
        }

        /// <summary>
        /// Bind the commands this window implements to their implementations (makes way more sense to do this in code than XAML to me?)
        /// </summary>
        private void AddCommandBindings()
        {
            CommandBindings.Add( new CommandBinding( Commands.CmdPreviewStereogram, CmdPreviewStereogramExecuted, CmdPreviewStereogramCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdGenerateStereogram, CmdGenerateStereogramExecuted, CmdGenerateStereogramCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdSaveStereogram, CmdSaveStereogramExecuted, CmdSaveStereogramCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdRestoreStereogramSettings, CmdRestoreStereogramSettingsExecuted, CmdRestoreStereogramSettingsCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdRegenerateStereogram, CmdRegenerateStereogramExecuted, CmdRegenerateStereogramCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdFullscreen, CmdFullscreenExecuted, CmdFullscreenCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdInvertDepthmap, CmdInvertDepthmapExecuted, CmdInvertDepthmapCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdAdjustDepthmapLevels, CmdAdjustDepthmapLevelsExecuted, CmdAdjustDepthmapLevelsCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdMergeDepthmaps, CmdMergeDepthmapsExecuted, CmdMergeDepthmapsCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdSaveDepthmap, CmdSaveDepthmapExecuted, CmdSaveDepthmapCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdDefaultSettings, CmdDefaultSettingsExecuted, CmdDefaultSettingsCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdDefaultResources, CmdDefaultResourcesExecuted, CmdDefaultResourcesCanExecute ) );
            CommandBindings.Add( new CommandBinding( Commands.CmdAboutBox, CmdAboutBoxExecuted, CmdAboutBoxCanExecute ) );
        }

        /// <summary>
        /// If a preview item is set, pass it on to the active preview pane
        /// </summary>
        /// <param name="item"></param>
        private void SetPreviewItem( object item )
        {
            if ( PreviewPane != null )
            {
                PreviewPane.SetPreviewItem( item );
            }
        }

        /// <summary>
        /// Save application settings on close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void windowMain_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            try
            {
                // Disable the progress reporter
                theViewModel.RegisterProgressReporter( null );

                // Save the filenames of any resources that are in palettes
                Properties.Settings.Default.Depthmaps = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Depthmaps.AddRange( theViewModel.DepthmapPalette.GetFilenames().ToArray() );

                Properties.Settings.Default.Textures = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Textures.AddRange( theViewModel.TexturePalette.GetFilenames().ToArray() );

                Properties.Settings.Default.Stereograms = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Stereograms.AddRange( theViewModel.StereogramPalette.GetFilenames().ToArray() );

                // Save the current selection... filenames might be null
                Properties.Settings.Default.SelectedDepthmap = ( theViewModel.SelectedDepthmap != null ) ? theViewModel.SelectedDepthmap.filename : null;
                Properties.Settings.Default.SelectedTexture = ( theViewModel.SelectedTexture != null ) ? theViewModel.SelectedTexture.filename : null;
                Properties.Settings.Default.SelectedStereogram = ( theViewModel.SelectedStereogram != null ) ? theViewModel.SelectedStereogram.filename : null;

                // Save the default resource directories
                if ( theViewModel.DepthmapPalette.sDefaultDirectory != null && theViewModel.DepthmapPalette.sDefaultDirectory.Length > 0 && Directory.Exists( theViewModel.DepthmapPalette.sDefaultDirectory ) )
                {
                    Properties.Settings.Default.DepthmapPath = theViewModel.DepthmapPalette.sDefaultDirectory;
                }

                if ( theViewModel.TexturePalette.sDefaultDirectory != null && theViewModel.TexturePalette.sDefaultDirectory.Length > 0 && Directory.Exists( theViewModel.TexturePalette.sDefaultDirectory ) )
                {
                    Properties.Settings.Default.TexturePath = theViewModel.TexturePalette.sDefaultDirectory;
                }

                if ( theViewModel.StereogramPalette.sDefaultDirectory != null && theViewModel.StereogramPalette.sDefaultDirectory.Length > 0 && Directory.Exists( theViewModel.StereogramPalette.sDefaultDirectory ) )
                {
                    Properties.Settings.Default.SterogramPath = theViewModel.StereogramPalette.sDefaultDirectory;
                }

                // Window pos + size
                if ( this.WindowState == System.Windows.WindowState.Normal )
                {
                    Properties.Settings.Default.Top = this.Top;
                    Properties.Settings.Default.Left = this.Left;
                    Properties.Settings.Default.Height = this.Height;
                    Properties.Settings.Default.Width = this.Width;                    
                }

                // Would be less code and more maintainable just to dump the Options object into an XML file ourselves... could databind to the properties, but it scares me
                Properties.Settings.Default.Algorithm = (int)Options.algorithm;
                Properties.Settings.Default.ResolutionX = Options.resolutionX;
                Properties.Settings.Default.ResolutionY = Options.resolutionY;
                Properties.Settings.Default.Depth = Options.FieldDepth;
                Properties.Settings.Default.Separation = Options.separation;
                Properties.Settings.Default.Oversample = (int)Options.oversample;
                Properties.Settings.Default.RemoveHiddenSurfaces = Options.bRemoveHiddenSurfaces;
                Properties.Settings.Default.AddConvergenceDots = Options.bAddConvergenceDots;
                Properties.Settings.Default.PreserveAspectRatio = Options.bPreserveAspectRatio;

                Properties.Settings.Default.Save();
            }
            catch ( Exception ex )
            {
                MessageBox.Show( String.Format( "Error saving settings: {0}", ex.Message ) );
            }
        }

       /// <summary>
        /// Add depthmaps to the library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddDepthmaps_Click( object sender, RoutedEventArgs e )
        {
            tabControlPalette.SelectedItem = tabItemDepthmaps;
        }

        /// <summary>
        /// When a tab on the palette is selected, display an appropriate image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControlPalette_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.Source is TabControl )
            {
                TabControl control = (TabControl)e.Source;
                TabItem tab = (TabItem)control.SelectedItem;
                theViewModel.SelectedPalette = ( (PaletteView)tab.Content ).Palette;

                if ( theViewModel.PreviewItem != theViewModel.PreviewStereogram )
                {
                    if ( theViewModel.SelectedPalette != null && theViewModel.SelectedPalette.GetSelectedThumbnail() != null )
                    {
                        theViewModel.PreviewItem = theViewModel.SelectedPalette.GetSelectedThumbnail();
                    }                                    
                }
            }

        }

        /// <summary>
        /// Callback for when SelectedPalette changes... MUST be a way to databind this?
        /// </summary>
        /// <param name="palette"></param>
        private void tabControlPalette_PaletteSelected( Palette palette )
        {
            if ( tabControlPalette != null )
            {
                foreach ( TabItem tab in tabControlPalette.Items )
                {
                    if ( tab.Content is PaletteView )
                    {
                        if ( ((PaletteView)tab.Content).Palette == palette )
                        {
                            tabControlPalette.SelectedItem = tab;
                            break;
                        }
                    }                    
                }                
            }
        }

        /// <summary>
        /// Keyboard handler for main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void windowMain_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Delete )
            {
                if ( theViewModel.SelectedPalette != null )
                {
                    theViewModel.SelectedPalette.RemoveSelectedItems();
                }
            }
        }


        private void windowMain_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            if ( PreviewPane == previewMainPanel )
            {
                theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );                
            }
        }

        private void comboBoxAlgorithm_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( PreviewPane != null )
            {
                theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );                
            }
        }

        private void comboBoxOversample_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PreviewPane != null)
            {
                theViewModel.UpdatePreview((int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500);
            }
        }

        private void CheckBox_Click( object sender, RoutedEventArgs e )
        {
            if ( PreviewPane != null )
            {
                theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );
            }
        }

        private void TextBox_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Return || e.Key == Key.Enter )
            {
                // If user presses return, update the bound property
                TextBox box = (TextBox)sender;
                if ( null != box.GetBindingExpression( TextBox.TextProperty ) )
                {
                    box.GetBindingExpression( TextBox.TextProperty ).UpdateSource();
                }
                box.SelectAll();

                if ( PreviewPane != null )
                {
                    theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );
                }
            }

        }

        private void TextBox_LostFocus( object sender, RoutedEventArgs e )
        {
            // Update the bound property explicitly, so we can use the correct value in the preview
            TextBox box = (TextBox)sender;
            if ( null != box.GetBindingExpression( TextBox.TextProperty ) )
            {
                box.GetBindingExpression( TextBox.TextProperty ).UpdateSource();
            }

            if ( PreviewPane != null )
            {
                theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );
            }
        }


        /// <summary>
        /// Generate a preview stereogram using the currently specified options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdPreviewStereogramExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Options previewOptions = new Options( Options );
            if ( e.Parameter is Stereogram && ( (Stereogram)e.Parameter ).HasOptions )
            {
                Stereogram src = (Stereogram)e.Parameter;
                previewOptions.texture = src.options.texture ?? theViewModel.SelectedTexture;
                previewOptions.depthmap = src.options.depthmap ?? theViewModel.SelectedDepthmap;
            }
            else
            {
                previewOptions.texture = ( e.Parameter as Texture ) ?? theViewModel.SelectedTexture;
                previewOptions.depthmap = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;
            }
            previewOptions.resolutionX = (int)PreviewPane.ActualWidth;
            previewOptions.resolutionY = (int)PreviewPane.ActualHeight;
            theViewModel.RequestPreview( previewOptions );
        }

        public void CmdPreviewStereogramCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            if ( PreviewPane != null )
            {
                if ( e.Parameter is Stereogram )
                {
                    e.CanExecute = ((Stereogram)e.Parameter).HasOptions;
                }
                else
                {
                    Texture tx = ( e.Parameter as Texture ) ?? theViewModel.SelectedTexture;
                    Depthmap dm = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;
                    e.CanExecute = ( tx != null && dm != null );
                }
            }
            else
            {
                e.CanExecute = false;
            }
        }

        /// <summary>
        /// Geneate a stereogram using an option selection dialog (and optionally save it to a file)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdGenerateStereogramExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Options.texture = ( e.Parameter as Texture ) ?? theViewModel.SelectedTexture;
            Options.depthmap = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;

            Options saveOptions = new Options( Options );

            DialogGenerateStereogram dialog = new DialogGenerateStereogram( saveOptions, theViewModel.GetDepthmaps(), theViewModel.GetTextures(), false );
            bool? ok = dialog.ShowDialog();

            if ( ok != true )
                return;

            bool bSave = dialog.SaveStereogram;

            // Adopt the options from the dialog if they were accepted
            this.DataContext = null;
            Options = saveOptions;
            this.DataContext = theViewModel;

            theViewModel.GenerateStereogram( saveOptions, stereogram => StereogramGeneratedCallback( stereogram, bSave ) );
        }

        public void CmdGenerateStereogramCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            Texture tx = ( e.Parameter as Texture ) ?? theViewModel.SelectedTexture;
            Depthmap dm = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;
            e.CanExecute = ( tx != null && dm != null );
        }

        /// <summary>
        /// Callback for generated stereograms
        /// </summary>
        /// <param name="?"></param>
        private void StereogramGeneratedCallback( Stereogram stereogram, bool bSave )
        {
            if ( stereogram == null || stereogram.Bitmap == null )
            {
                MessageBox.Show( "Error generating stereogram", "Error generating stereogram", MessageBoxButton.OK );
                return;
            }

            // Highlight it
            theViewModel.PreviewItem = stereogram;

            if ( bSave )
            {
                try
                {
                    theViewModel.SaveStereogram( stereogram );
                }
                catch ( Exception ex )
                {
                    MessageBox.Show( ex.Message, "Error saving file", MessageBoxButton.OK );
                }
            }

            MessageBox.Show( String.Format( "Generated Stereogram in {0}ms", stereogram.Milliseconds.ToString() ) );
        }


        /// <summary>
        /// Regenerate stereogram using the same options (but with a dialog to allow you to change them, or there'd really be no point)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdRegenerateStereogramExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Stereogram src = e.Parameter as Stereogram;

            if ( src == null || src.HasOptions == false )
            {
                throw new ArgumentException( "Cannot regenerate stereogram" );                
            }

            Options saveOptions = new Options( src.options );

            DialogGenerateStereogram dialog = new DialogGenerateStereogram( saveOptions, theViewModel.GetDepthmaps(), theViewModel.GetTextures(), false );
            bool? ok = dialog.ShowDialog();

            if ( ok != true )
                return;

            bool bSave = dialog.SaveStereogram;

            // Adopt the options from the dialog if they were accepted
            this.DataContext = null;
            Options = saveOptions;
            this.DataContext = theViewModel;

            theViewModel.GenerateStereogram( saveOptions, stereogram => StereogramGeneratedCallback( stereogram, bSave ) );
        }

        public void CmdRegenerateStereogramCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = ( e.Parameter is Stereogram ) && ( (Stereogram)e.Parameter ).HasOptions;
        }


        /// <summary>
        /// Save an already generated stereogram to a file... should really pop up the options box
        /// to confirm settings and allow tweaking.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdSaveStereogramExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Stereogram stereogram = ( e.Parameter as Stereogram ) ?? theViewModel.SelectedStereogram;

            if ( null == stereogram || false == stereogram.HasOptions )
                return;

            try
            {
                theViewModel.SaveStereogram( stereogram );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error saving file", MessageBoxButton.OK );
            }
        }

        public void CmdSaveStereogramCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            Stereogram stereogram = ( e.Parameter as Stereogram ) ?? theViewModel.SelectedStereogram;
            e.CanExecute = ( stereogram != null );
        }

        // Restore settings from a previously generated stereogram
        public void CmdRestoreStereogramSettingsExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Stereogram stereogram = ( e.Parameter as Stereogram ) ?? theViewModel.SelectedStereogram;
            if ( stereogram.options != null )
            {
                this.DataContext = null;
                theViewModel.RestoreStereogramSettings( stereogram );
                this.DataContext = theViewModel;        // Rebind controls
            }
        }

        public void CmdRestoreStereogramSettingsCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            Stereogram stereogram = ( e.Parameter as Stereogram ) ?? theViewModel.SelectedStereogram;
            e.CanExecute = ( stereogram != null && stereogram.options != null );
        }


        // Full-screen view
        public void CmdFullscreenExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            if ( e.Parameter is BitmapType || e.Parameter is Thumbnail )
            {
                BitmapType target = (BitmapType)e.Parameter;
                PreviewPane restore = PreviewPane;
                FullscreenView fsview = new FullscreenView( target, () => PreviewPane = restore );
                fsview.Show();
                PreviewPane = fsview.previewFullScreen;
            }
        }

        public void CmdFullscreenCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = ( e.Parameter is BitmapType ) || ( e.Parameter is Thumbnail );
        }

        // Invert a depthmap
        public void CmdInvertDepthmapExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Depthmap dm = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;

            if ( dm != null )
            {
                try
                {
                    Depthmap dmi = theViewModel.InvertDepthmap( dm );

                    if ( dmi != null )
                    {
                        theViewModel.SaveBitmapToFile( dmi, "Save Depthmap", theViewModel.DepthmapPalette.sDefaultDirectory );
                    }
                }
                catch ( Exception ex )
                {
                    MessageBox.Show( ex.Message, "Error" );
                }
            }
        }

        public void CmdInvertDepthmapCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = ( ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap ) != null;
        }

        // Adjust depthmap levels
        public void CmdAdjustDepthmapLevelsExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            Depthmap dm = ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap;

            try
            {
                LevelAdjust dlg = new LevelAdjust( dm );
                dlg.ShowDialog();

                if ( dlg.DialogResult == true )
                {
                    Depthmap dma = theViewModel.AdjustDepthmapLevels( dm, dlg.adjustments );

                    if ( dma != null )
                    {
                        theViewModel.SaveBitmapToFile( dma, "Save Depthmap", theViewModel.DepthmapPalette.sDefaultDirectory );
                    }
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error" );
            }
        }

        public void CmdAdjustDepthmapLevelsCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = ( ( e.Parameter as Depthmap ) ?? theViewModel.SelectedDepthmap ) != null;
        }

        // Merge 2 or more depthmaps together
        public void CmdMergeDepthmapsExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            try
            {
                BitmapType primary = theViewModel.DepthmapPalette.GetSelectedItem();
                List<BitmapType> items = theViewModel.DepthmapPalette.GetMultiselectedItems();
                items.Remove( primary );

                if ( items.Count > 0 )
                {
                    Depthmap dm = theViewModel.MergeDepthmaps( primary, items );

                    if ( dm != null )
                    {
                        theViewModel.SaveBitmapToFile( dm, "Save Depthmap", theViewModel.DepthmapPalette.sDefaultDirectory );                    
                    }
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error" );
            }
        }

        public void CmdMergeDepthmapsCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            // should probably check whether the parameter thumbnail is one of the multiselected thumbs, for intuitive interface
            e.CanExecute = theViewModel.DepthmapPalette.GetMultiselectedItems().Count > 1;
        }


        /// <summary>
        /// Save image to a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdSaveDepthmapExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            try
            {
                BitmapType image = e.Parameter as BitmapType;
                if ( image != null )
                {
                    theViewModel.SaveBitmapToFile( image, "Save Depthmap", theViewModel.DepthmapPalette.sDefaultDirectory );
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error" );
            }
        }

        public void CmdSaveDepthmapCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            BitmapType image = e.Parameter as BitmapType;
            e.CanExecute = ( image != null );
        }

        /// <summary>
        /// Restore default settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdDefaultSettingsExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            // Well this is vaguely disgusting... null out the datacontext, then force it to rebind when the options have changed.
            // Certainly simpler + cleaner than introducing dependency properties for all the options though
            this.DataContext = null;
            theViewModel.Options = new Options();
            theViewModel.Options.depthmap = theViewModel.SelectedDepthmap;
            theViewModel.Options.texture = theViewModel.SelectedTexture;
            this.DataContext = theViewModel;

            if ( PreviewPane != null )
            {
                theViewModel.UpdatePreview( (int)PreviewPane.ActualWidth, (int)PreviewPane.ActualHeight, 500 );
            }
        }

        public void CmdDefaultSettingsCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            // Any circumstances where this wouldn't be admissable?
            e.CanExecute = true;
        }

        /// <summary>
        /// Add default resources to a palette
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CmdDefaultResourcesExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            try
            {
                if ( e.Parameter == theViewModel.DepthmapPalette )
                {
                    theViewModel.DefaultDepthmaps();
                }
                else if ( e.Parameter == theViewModel.TexturePalette )
                {
                    theViewModel.DefaultTextures();
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error populating resources" );
            }
        }

        public void CmdDefaultResourcesCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            if ( ( e.Parameter == theViewModel.DepthmapPalette ) || ( e.Parameter == theViewModel.TexturePalette ) )
            {
                e.CanExecute = true;                
            }
        }


        public void CmdAboutBoxExecuted( object sender, ExecutedRoutedEventArgs e )
        {
            AboutBox box = new AboutBox( license );
            box.ShowDialog();
        }

        public void CmdAboutBoxCanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = true;
        }

    }

}
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

using Microsoft.Win32;

using Stereogrammer.Model;

namespace Stereogrammer.ViewModel
{
    using Algorithm = StereogramAlgorithm.Algorithm;

    class StereogrammerViewModel
    {
        public Options Options { get; set; }

        public DepthmapCollection myDepthmaps = new DepthmapCollection();
        public TextureCollection myTextures = new TextureCollection();
        public StereogramCollection myStereograms = new StereogramCollection();

        Depthmap depthmapFlat = null;
        Texture textureRandomDots = null;
        Texture textureRandomColours = null;

        public Palette DepthmapPalette { get; set; }
        public Palette TexturePalette { get; set; }
        public Palette StereogramPalette { get; set; }

        public ProgressMonitor monitor;

        // Keep an observable collection of palettes... could databind tab control to the collection,
        // But lose a fair amount of flexibility in the XAML by doing so
        public ObservableCollection<Palette> Palettes { get; private set; }

        /// <summary>
        /// Callback for when a palette is selected
        /// </summary>
        public event Action<Palette> OnPaletteSelected;

        /// <summary>
        /// Callback for when a new object is set for previewing
        /// </summary>
        public event Action<object> OnPreviewItemChanged;

        /// <summary>
        /// Callback for displaying an error message
        /// </summary>
        public event Action<string> OnErrorMessage;

        /// <summary>
        /// Item set for previewing
        /// </summary>
        private object _PreviewItem = null;
        public object PreviewItem { 
            get { return _PreviewItem; }
            set
            {
                if ( _PreviewItem != value )
                {
                    _PreviewItem = value;
                    if ( OnPreviewItemChanged != null )
                    {
                        OnPreviewItemChanged( _PreviewItem );
                    }                
                }
            }
        }

        // Would like to databind selected palette to something in the view, but a callback is more flexible
        Palette _selectedPalette = null;
        public Palette SelectedPalette
        {
            get { return _selectedPalette; }
            set { 
                if ( _selectedPalette != value )
                {
                    _selectedPalette = value;
                    if ( OnPaletteSelected != null )
                    {
                        OnPaletteSelected( _selectedPalette );
                    }                
                }
            }
        }

        /// <summary>
        /// Get the item which has been selected in the Texture Palette
        /// </summary> 
        public Texture SelectedTexture
        {
            get
            {
                if ( TexturePalette != null && TexturePalette.GetSelectedThumbnail() != null )
                {
                    Thumbnail thumb = TexturePalette.GetSelectedThumbnail();
                    return (Texture)thumb.ThumbnailOf;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the item which has been selected in the Depthmap Palette
        /// </summary>
        public Depthmap SelectedDepthmap
        {
            get
            {
                if ( DepthmapPalette != null && DepthmapPalette.GetSelectedThumbnail() != null )
                {
                    Thumbnail thumb = DepthmapPalette.GetSelectedThumbnail();
                    return (Depthmap)thumb.ThumbnailOf;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the item which has been selected in the Stereogram Palette
        /// </summary>
        public Stereogram SelectedStereogram
        {
            get
            {
                if ( StereogramPalette != null && StereogramPalette.GetSelectedThumbnail() != null )
                {
                    Thumbnail thumb = StereogramPalette.GetSelectedThumbnail();
                    return (Stereogram)thumb.ThumbnailOf;
                }
                return null;
            }
        }

        // Logical solution to this still elusive
        StereogramGeneratorAsync previewer = null;
        Stereogram _PreviewStereogram = null;
        public Stereogram PreviewStereogram
        {
            get
            {
                return _PreviewStereogram;
            }
            private set
            {
                _PreviewStereogram = value;
                PreviewItem = _PreviewStereogram;
            }
        }

        /// <summary>
        /// Callback type for generated stereograms
        /// </summary>
        /// <param name="stereogram"></param>
        public delegate void StereogramGenerated( Stereogram stereogram );

        /// <summary>
        /// Constructor for the view model - binds to the model
        /// </summary>
        /// <param name="model"></param>
        public StereogrammerViewModel()
        {
            Options = new Options();

            DepthmapPalette = AddPalette( myDepthmaps, Commands.CmdPreviewStereogram );
            TexturePalette = AddPalette( myTextures, Commands.CmdPreviewStereogram );
            StereogramPalette = AddPalette( myStereograms, Commands.CmdPreviewStereogram );

            previewer = new StereogramGeneratorAsync( new Action<Stereogram>( stereogram => 
                    {
                        if ( stereogram != null )
                            this.PreviewStereogram = stereogram;
                        else
                            ErrorMessage( "Preview failed!" );
                        EndMonitoring(); 
                    } ) );
        }

        /// <summary>
        /// Register a palette
        /// </summary>
        /// <param name="p"></param>
        /// <param name="doubleClick"></param>
        private Palette AddPalette( BitmapCollection collection, RoutedCommand doubleClick = null )
        {
            Palette p = new Palette( collection );

            if ( doubleClick != null )
            {
                p.DefaultDoubleClick = doubleClick;                
            }
            p.OnThumbnailSelected += event_ThumbnailSelected;

            if ( Palettes == null )
            {
                Palettes = new ObservableCollection<Palette>();
            }
            Palettes.Add( p );
            return p;
        }


        /// <summary>
        /// Accessors to deeper levels
        /// </summary>
        /// <returns></returns>
        public List<BitmapType> GetDepthmaps()
        {
            return DepthmapPalette.GetItems();
        }

        public List<BitmapType> GetTextures()
        {
            return TexturePalette.GetItems();
        }

        /// <summary>
        /// Helper to generate a stereogram
        /// </summary>
        /// <param name="options"></param>
        /// <param name="bSave"></param>
        /// <param name="bAddThumbnail"></param>
        /// <returns></returns>
        public void GenerateStereogram( Options options, StereogramGenerated callback = null )
        {
            StereogramGeneratorAsync generator = new StereogramGeneratorAsync( stereogram => OnStereogramGenerated( stereogram, callback ) );
            generator.RequestStereogram( options );
            MonitorProgress( () => (float)generator.GetProgress() );
        }

        private void OnStereogramGenerated( Stereogram stereogram, StereogramGenerated callback )
        {
            if ( stereogram != null )
            {
                myStereograms.AddItem( stereogram );
                SelectedPalette = StereogramPalette;
            }

            if ( callback != null )
            {
                callback( stereogram );
            }

            EndMonitoring();
        }

        /// <summary>
        /// Helper to generate a preview stereogram - at the moment there is only one generator, so
        /// requests for multiple previews will pre-empt each other.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="bSave"></param>
        /// <param name="bAddThumbnail"></param>
        /// <returns></returns>
        public StereogramGeneratorAsync RequestPreview( Options options, long millisecondDelay = 0 )
        {
            previewer.RequestStereogram( options, millisecondDelay );
            MonitorProgress( () => (float)previewer.GetProgress() );
            return previewer;
        }

        /// <summary>
        /// Update the preview pane... i.e. if the preview pane is showing the preview stereogram, regenerate it
        /// </summary>
        /// <param name="millisecondDelay"></param>
        public void UpdatePreview( int PreviewWidth, int PreviewHeight, long millisecondDelay = 0 )
        {
            if ( PreviewItem != null && PreviewItem == PreviewStereogram && PreviewStereogram.HasOptions )
            {
                Options previewOptions = new Options( Options );
                previewOptions.depthmap = PreviewStereogram.options.depthmap;
                previewOptions.texture = PreviewStereogram.options.texture;
                previewOptions.resolutionX = PreviewWidth;
                previewOptions.resolutionY = PreviewHeight;
                RequestPreview( previewOptions, millisecondDelay );
            }
        }

        /// <summary>
        /// Register a progress reporter, e.g. a status bar
        /// </summary>
        /// <param name="report"></param>
        /// <param name="complete"></param>
        /// <returns></returns>
        public ProgressMonitor RegisterProgressReporter( ProgressMonitor.ReportStatus report, ProgressMonitor.OnStart start = null, ProgressMonitor.OnCompletion complete = null )
        {
            if ( monitor != null )
            {
                monitor.EndMonitoring();
                monitor.Dispose();
                monitor = null;
            }

            if ( report != null )
            {
                monitor = new ProgressMonitor( report, start, complete );
            }

            return monitor;
        }

        /// <summary>
        /// Start reporting progress of an operation
        /// </summary>
        /// <param name="progress"></param>
        public void MonitorProgress( ProgressMonitor.UpdateProgress progress )
        {
            if ( monitor != null )
            {
                monitor.MonitorProgress( progress );
            }
        }

        public void EndMonitoring()
        {
            if ( monitor != null )
            {
                monitor.EndMonitoring();
            }
        }



        /// <summary>
        /// Save a stereogram
        /// </summary>
        /// <param name="stereogram"></param>
        public void SaveStereogram( Stereogram stereogram )
        {
            Debug.Assert( stereogram != null );
            SaveBitmapToFile( stereogram, "Save Stereogram", StereogramPalette.sDefaultDirectory );
        }

        /// <summary>
        /// Restore settings from a stereogram
        /// </summary>
        /// <param name="stereogram"></param>
        public void RestoreStereogramSettings( Stereogram stereogram )
        {
            if ( stereogram != null && stereogram.options != null )
            {
                DepthmapPalette.SelectItem( stereogram.options.depthmap );
                TexturePalette.SelectItem( stereogram.options.texture );
                Options = new Options( stereogram.options );
                PreviewItem = stereogram;
            }
        }

        /// <summary>
        /// Save the thumbnailable item to an image file, using a SaveFileDialog and
        /// deducing the file type from the specified file extension.  Mixing business logic
        /// and presentation up even more, but it does seem to be the 'natural' place for the function.
        /// </summary>
        /// <param name="dialogTitle"></param>
        /// <param name="initialDirectory"></param>
        public void SaveBitmapToFile( BitmapType bitmap, string dialogTitle, string initialDirectory )
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.InitialDirectory = initialDirectory;
            saveDialog.FileName = bitmap.Name;
            saveDialog.OverwritePrompt = true;
            saveDialog.Title = dialogTitle;
            saveDialog.ValidateNames = true;
            saveDialog.Filter = "JPG file (*.jpg)|*.jpg|BMP file (*.bmp)|*.bmp|PNG file (*.png)|*.png";

            bool? result = saveDialog.ShowDialog();

            if ( result != true )
                return;

            FileType[] types = { FileType.JPG, FileType.BMP, FileType.PNG };

            int type = Math.Max( 0, saveDialog.FilterIndex - 1 );

            if ( type >= types.Length )
            {
                throw new ArgumentException( "Invalid file type" );
            }

            FileInfo file = new FileInfo( saveDialog.FileName );
            DirectoryInfo directory = file.Directory;

            if ( directory.Exists == false )
            {
                throw new ArgumentException( "Invalid directory" );
            }

            bitmap.SaveImage( file.FullName, types[ type ] );
        }

        /// <summary>
        /// Return a new depthmap which is the depth-inversion of the presented depthmap
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        public Depthmap InvertDepthmap( Depthmap dm )
        {
            BitmapSource inverted = dm.GetLevelInverted();
            Depthmap dmi = new Depthmap( inverted );
            dmi.Name = dm.Name + "_inverted";
            myDepthmaps.AddItem( dmi );
            return dmi;
        }

        /// <summary>
        /// Return a new depthmap with adjusted levels
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        public Depthmap AdjustDepthmapLevels( Depthmap dm, LevelAdjustments adjustments )
        {
            BitmapSource adjusted = dm.GetLevelAdjusted( adjustments );
            Depthmap dma = new Depthmap( adjusted );
            dma.Name = dm.Name + "_adjusted";
            myDepthmaps.AddItem( dma );
            return dma;
        }

        /// <summary>
        /// Merge multiple depthmaps into a new depthmap by comparing Z values and taking the closest.
        /// Output resolution will always be the same as the primary depthmap
        /// </summary>
        /// <param name="depthmaps"></param>
        /// <returns></returns>
        public Depthmap MergeDepthmaps( BitmapType primary, List<BitmapType> others )
        {
            Depthmap dm = primary as Depthmap;
            if ( primary != null )
            {
                foreach ( var item in others )
                {
                    Depthmap dm2 = item as Depthmap;
                    if ( item != null && item != primary )
                    {
                        Depthmap dmm = dm.MergeWith( dm2 );
                        dmm.Name = dm.Name + "+" + dm2.Name;
                        dm = dmm;                        
                    }                    
                }
                myDepthmaps.AddItem( dm );
                return dm;
            }
            else
            {
                throw new ArgumentException( "No depthmaps to merge" );
            }
        }

        /// <summary>
        /// Event handler for palette item selected
        /// </summary>
        /// <param name="palette"></param>
        /// <param name="thumb"></param>
        private void event_ThumbnailSelected( Palette palette, Thumbnail thumb )
        {
            PreviewItem = thumb;
        }

        public void Populate()
        {
            depthmapFlat = new Depthmap( 32, 32 );
            depthmapFlat.Name = "Flat";
            myDepthmaps.AddItem( depthmapFlat, bCanRemove: false );

            textureRandomDots = new TextureGreyDots( (int)Options.separation, (int)Options.separation );
            textureRandomDots.Name = "Random Dots";
            myTextures.AddItem( textureRandomDots, bCanRemove: false );
            textureRandomColours = new TextureColourDots( (int)Options.separation, (int)Options.separation );
            textureRandomColours.Name = "Random Coloured Dots";
            myTextures.AddItem( textureRandomColours, bCanRemove: false );

            // Restore any depthmaps saved in the settings (sanity check on max count incase settings get screwed)
            if ( Properties.Settings.Default.Depthmaps != null && Properties.Settings.Default.Depthmaps.Count > 0 && Properties.Settings.Default.Depthmaps.Count < 1000 )
            {
                myDepthmaps.Populate( Properties.Settings.Default.Depthmaps );
            }
            else
            {
                DefaultDepthmaps();
            }

            // Restore any depthmaps saved in the settings
            if ( Properties.Settings.Default.Textures != null && Properties.Settings.Default.Textures.Count > 0 && Properties.Settings.Default.Textures.Count < 1000 )
            {
                myTextures.Populate( Properties.Settings.Default.Textures );
            }
            else
            {
                DefaultTextures();
            }

            // Could do with saving the sterograms as custom objects, with their settings inside... or add custom metadata to the files?
            if ( Properties.Settings.Default.Stereograms != null && Properties.Settings.Default.Stereograms.Count > 0 && Properties.Settings.Default.Stereograms.Count < 1000 )
            {
                myStereograms.Populate( Properties.Settings.Default.Stereograms );
            }
        }

        /// <summary>
        /// Populate Depthmap palette with default resources
        /// </summary>
        public void DefaultDepthmaps()
        {
            myDepthmaps.Clear();

            string[] resources = { @"pack://application:,,,/Images/3D2.png",
                                        @"pack://application:,,,/Images/3dbubbles.png",
                                        @"pack://application:,,,/Images/bumps1.png",
                                        @"pack://application:,,,/Images/oddone3.png",
                                        @"pack://application:,,,/Images/ripple2.png",
                                        @"pack://application:,,,/Images/ripple4.png",
                                        @"pack://application:,,,/Images/sombrero.png",
                                        @"pack://application:,,,/Images/sphere4.png",
                                        @"pack://application:,,,/Images/volcano.png"
                                        };

            myDepthmaps.Populate( resources );
        }

        /// <summary>
        /// Populate Texture palette with default resources
        /// </summary>
        public void DefaultTextures()
        {
            myTextures.Clear();

            string[] resources = { @"pack://application:,,,/Images/chrome_refraction.jpg",
                                       @"pack://application:,,,/Images/chunky_spinach.jpg",
                                       @"pack://application:,,,/Images/dendrite_dance.jpg",
                                       @"pack://application:,,,/Images/distorted_anomaly.jpg",
                                       @"pack://application:,,,/Images/glowing_wildebeast.jpg",
                                       @"pack://application:,,,/Images/NuclearCoral.jpg",
                                       @"pack://application:,,,/Images/thin_tentacles.jpg"
                                     };

            myTextures.Populate( resources );
        }

        /// <summary>
        /// Proxy for displaying an error message
        /// </summary>
        /// <param name="message"></param>
        private void ErrorMessage( string message )
        {
            if ( OnErrorMessage != null )
            {
                OnErrorMessage( message );
            }
        }


    }
}

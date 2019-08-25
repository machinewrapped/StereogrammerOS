// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;

namespace Stereogrammer.Model
{
    /// <summary>
    /// A collection of Bitmap Types.
    /// Doesn't actually add much to the model layer, and could (maybe should) be rolled into Palette at the ViewModel layer for simplification.
    /// </summary>
    public class BitmapCollection
    {
        Func<BitmapImage, BitmapType> FactoryFunc { get; set; }

        List<BitmapType> myItems = new List<BitmapType>();

        public delegate void ItemCallback( BitmapCollection collection, BitmapType item );

        public event ItemCallback OnItemAdded;
        public event ItemCallback OnItemRemoved;

        public BitmapCollection( Func<BitmapImage, BitmapType> factoryFunc )
        {
            this.FactoryFunc = factoryFunc;
        }

        /// <summary>
        /// Add a BitmapType object to the collection
        /// </summary>
        /// <param name="item"></param>
        public void AddItem( BitmapType item, bool bCanRemove = true )
        {
            item.bCanRemove = bCanRemove;
            myItems.Add( item );
            if ( null != OnItemAdded )
            {
                OnItemAdded( this, item );
            }
        }

        /// <summary>
        /// Create a BitmapType object from a BitmapImage using the factory function, and add it to the collection
        /// </summary>
        /// <param name="item"></param>
        public BitmapType AddNewItem( BitmapImage bitmap )
        {
            BitmapType item = (BitmapType)FactoryFunc( bitmap );
            AddItem( item );
            return item;
        }

        /// <summary>
        /// Remove an item from the collection
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem( BitmapType item )
        {
            if ( myItems.Remove( item ) )
            {
                if ( null != OnItemRemoved )
                {
                    OnItemRemoved( this, item );                    
                }
            }            
        }

        /// <summary>
        /// Accessors for lists for data-binding purposes
        /// </summary>
        /// <returns></returns>
        public List<BitmapType> GetItems()
        {
            List<BitmapType> items = new List<BitmapType>();
            foreach ( var item in myItems )
                items.Add( item );
            return items;
        }

        public List<string> GetItemNames()
        {
            List<string> names = new List<string>();
            foreach ( var item in myItems )
                names.Add( item.Name );
            return names;
        }

        public List<string> GetFilenames()
        {
            List<string> filenames = new List<string>();
            foreach ( var item in myItems )
            {
                if ( item.filename != null )
                {
                    filenames.Add( item.filename );
                }
            }
            return filenames;
        }

        /// <summary>
        /// Create a BitmapType from a file.  Must have set the factory function to use.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public BitmapType CreateItemFromFile( string name, Uri uri )
        {
            BitmapImage image = new BitmapImage( uri );
            BitmapType x = AddNewItem( image );
            x.Name = name;
            if ( uri.IsFile )
            {
                x.filename = Uri.UnescapeDataString( uri.AbsolutePath );
            }
            else
            {
                x.filename = uri.AbsoluteUri;
            }
            return x;
        }

        public BitmapType CreateItemFromResource( string name, string path )
        {
            Uri uri = new Uri( new Uri( @"pack://application:,,,/MyAssembly;component" ), path );
            BitmapImage image = new BitmapImage( uri );
            BitmapType x = AddNewItem( image );
            x.Name = name;
            x.filename = uri.AbsoluteUri;
            return x;
        }

        /// <summary>
        /// Populate the palette from a list of filenames
        /// </summary>
        /// <param name="filenames"></param>
        public void Populate( string[] filenames )
        {
            foreach ( string filename in filenames )
            {
                try
                {
                    Uri uri = new Uri( filename );
                    if ( uri.IsFile )
                    {
                        FileInfo file = new FileInfo( filename );
                        string name = System.IO.Path.GetFileNameWithoutExtension( file.Name );
                        BitmapType item = CreateItemFromFile( name, uri );
                    }
                    else
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension( uri.LocalPath );
                        BitmapType item = CreateItemFromFile( name, uri );
                    }
                }
                catch ( Exception )
                {
                    Console.WriteLine( "Failed to load {0}", filename );
                }
            }
        }

        public void Populate( System.Collections.Specialized.StringCollection filenames )
        {
            string[] strings = new string[ filenames.Count ];
            filenames.CopyTo( strings, 0 );
            Populate( strings );
        }

        /// <summary>
        /// Clear the collection, except for any items marked as unremovable
        /// </summary>
        public void Clear()
        {
            BitmapType[] items = myItems.ToArray();
            foreach ( var x in items )
            {
                if ( x.bCanRemove )
                {
                    RemoveItem( x );
                }
            }
        }
    
    }
}

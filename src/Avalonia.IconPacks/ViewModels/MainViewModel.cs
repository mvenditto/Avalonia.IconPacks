﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Avalonia.IconPacks.ViewModels
{
    public class MainViewModel:ViewModelBase
    {
        public List<IconVM> Icons{ get; set; } = new List<IconVM>();
        public List<IconVM> _FilteredIcons;
        private string _StyleSourceCode = "";
        private string _SearchText = "";
        public ObservableCollection<IconVM> SelectedIcons { get; set; } = new ObservableCollection<IconVM>();

        public MainViewModel()
        {
            loadAllIcons();
         
            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => Search());

            this.WhenAnyValue(x => x.StyleSourceCode)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => CreateSelectedIcons());
        }
        void loadAllIcons()
        {
            foreach (var path in Directory.EnumerateFiles("Icons", "*.xaml"))
            {
                loadIcons(File.Open(path, FileMode.Open),Icons);
            }
        }

        
        private void loadIcons(Stream stream, IList<IconVM> output)
        {
            // crude deserializer
            // todo try using XamlIl for this
         
            XNamespace nsx = "http://schemas.microsoft.com/winfx/2006/xaml";
            XmlReader reader = XmlReader.Create(stream);
                
            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    var tag = reader.Name;
                    
                    if (tag == "GeometryDrawing" || tag == "DrawingGroup")
                    {
                        XElement el = XNode.ReadFrom(reader) as XElement;
                        if (el != null)
                        {
                            try
                            {
                                var s = el.Name;
                                if (tag == "GeometryDrawing")
                                {
                                    output.Add(new IconVM()
                                    {
                                        Name = el.Attribute(nsx + "Key").Value,
                                        drawing = new GeometryDrawing()
                                        {
                                            Brush = Brush.Parse(el.Attribute("Brush").Value),
                                            Geometry = Geometry.Parse(el.Attribute("Geometry").Value)
                                        },
                                        SourceCode = el.ToString()
                                    });
                                }
                                else
                                {
                                    var drawing = new DrawingGroup();
                                    foreach (var e in el.Elements())
                                    {
                                        if (e.Name.LocalName=="GeometryDrawing")
                                        {
                                            drawing.Children.Add(new GeometryDrawing()
                                            {
                                                Brush = Brush.Parse(e.Attribute("Brush").Value),
                                                Geometry = Geometry.Parse(e.Attribute("Geometry").Value)
                                            });
                                        }
                                    }
                                    
                                    output.Add(new IconVM()
                                    {
                                        Name = el.Attribute(nsx + "Key").Value,
                                        SourceCode = el.ToString(),
                                        drawing = drawing
                                    });
                                }
                            }
                            catch (Exception)
                            {
                            //    StyleSourceCode += "Error parsing icon\r\n" + el.ToString();
                            }
                        }
                    }
                }
            }
        }

        private void loadIconsOld(Stream stream, IList<IconVM> output)
        {
            // crude deserializer
            // todo try using XamlIl for this
            // can't easily read source and elements at once with XmlReader :(
            XmlReader reader = XmlReader.Create(stream);

            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "GeometryDrawing")
                    {

                        var icon = new IconVM();
                        var drawing = new GeometryDrawing();
                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "Brush": drawing.Brush = Brush.Parse(reader.Value); break;
                                case "Geometry": drawing.Geometry = Geometry.Parse(reader.Value); break;
                                case "x:Key": icon.Name = reader.Value; break;
                            }
                        }
                        icon.drawing = drawing;
                        output.Add(icon);
                    }
                    else if (reader.Name == "DrawingGroup")
                    {
                        var icon = new IconVM();
                        var drawingGroup = new DrawingGroup();
                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "x:Key": icon.Name = reader.Value; break;
                            }
                        }
                        do
                        {
                            reader.Read();
                            if (reader.Name == "GeometryDrawing")
                            {
                                var drawing = new GeometryDrawing();
                                while (reader.MoveToNextAttribute())
                                {
                                    switch (reader.Name)
                                    {
                                        case "Brush": drawing.Brush = Brush.Parse(reader.Value); break;
                                        case "Geometry": drawing.Geometry = Geometry.Parse(reader.Value); break;
                                    }
                                }
                                drawingGroup.Children.Add(drawing);
                            }
                        } while (reader.NodeType != XmlNodeType.EndElement);
                        icon.drawing = drawingGroup;
                        output.Add(icon);
                    }
                }
            }
        }

        public string StyleSourceCode
        {
            get
            {
                return _StyleSourceCode;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _StyleSourceCode, value);
                
            }
        }
        public List<IconVM> FilteredIcons
        {
            get
            {
                return _FilteredIcons;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _FilteredIcons, value);

            }
        }
        public string SearchText
        {
            get
            {
                return _SearchText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _SearchText, value);

            }
        }
        public IconVM SelectedIcon
        {
            get
            {
                return null;
            }
            set
            {
                AddToStyle(value);
            }
        }
        public void AddToStyle(IconVM icon)
        {
            if (icon != null)
            {
                var source = icon.SourceCode.Replace("p1:", "x:")
                   .Replace("xmlns:p1=\"http://schemas.microsoft.com/winfx/2006/xaml\"", "")
                   .Replace("xmlns=\"https://github.com/avaloniaui\"", "")
                   .Replace("\t", "  "); ;

                StyleSourceCode += "\r\n" +source;
               // SelectedIcons.Add(icon);
            }
        }
        public void CreateSelectedIcons()
        {
            // convert StyleSourceCode to a collection of drawings
            try
            {
                SelectedIcons.Clear();
                if (!String.IsNullOrEmpty(StyleSourceCode))
                {
                   var xaml = "<Styles xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" >"
                        + StyleSourceCode
                        + "</Styles>";
                    loadIcons(new MemoryStream(Encoding.UTF8.GetBytes(xaml)), SelectedIcons);                    
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void Search()
        {
            if (SearchText == "")
            {
                FilteredIcons = Icons;
            }
            else
            {
                FilteredIcons = Icons.FindAll(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
        }  
    }
    static class StringUtils
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
        }
    }
}
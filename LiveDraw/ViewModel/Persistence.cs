﻿using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace AntFu7.LiveDraw
{
    public class Persistence : NotifyPropertyChangedBase
    {
        public bool Topmost { get; set; }

        public bool MultiScreen { get; set; }

        public bool Orientation { get; set; }

        public bool LineMode { get; set; }

        public int PaletteX { get; set; } = Globals.DEFAULT_PALETTE_X;

        public int PaletteY { get; set; } = Globals.DEFAULT_PALETTE_Y;

        public string Color { get; set; } = "Black";

        public string ColorSelection { get; set; } = "White";

        public int BrushIndex { get; set; } = 2;

        #region Json singleton

        private static readonly string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiveDraw", "Persist.json");

        private static Persistence instance;

        public static Persistence Instance
        {
            get
            {
                if (instance == null)
                {
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            instance = JsonConvert.DeserializeObject<Persistence>(File.ReadAllText(fileName));
                        }
                        catch { }
                    }

                    if (instance == null)
                    {
                        instance = new Persistence();
                        instance.Save();
                    }

                    instance.PropertyChanged += instance.Instance_PropertyChanged;
                }

                return instance;
            }
        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            try
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(this));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "errorTitle", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private Persistence()
        { }

        #endregion Json singleton
    }
}

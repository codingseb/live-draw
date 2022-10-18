using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AntFu7.LiveDraw
{
    public class Persistence : INotifyPropertyChanged
    {
        public bool Topmost { get; set; }

        public bool MultiScreen { get; set; }

        #region Json singleton

        private static readonly string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiveDraw", "Persist.json");

        private static Persistence instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

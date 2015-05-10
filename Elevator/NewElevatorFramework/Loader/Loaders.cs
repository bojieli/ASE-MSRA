using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Loader
{
    public class PassengerLoader
    {
        string _fileName;

        public PassengerLoader(string xmlFile)
        {
            _fileName = xmlFile;
        }

        public Loader.Passengers Load()
        {
            XmlReader reader = XmlReader.Create(_fileName);
            XmlSerializer xs = new XmlSerializer(typeof(Loader.Passengers));
            Loader.Passengers ps = (Loader.Passengers)xs.Deserialize(reader);
            return ps;
        }

    }
    public class ElevatorLoader
    {
        string _fileName;
        public ElevatorLoader(string xmlFile)
        {
            _fileName = xmlFile;
        }

        public Loader.Elevators Load()
        {
            XmlReader reader = XmlReader.Create(_fileName);
            XmlSerializer xs = new XmlSerializer(typeof(Loader.Elevators));
            Loader.Elevators es = (Elevators)xs.Deserialize(reader);
            return es;
        }
    }
}

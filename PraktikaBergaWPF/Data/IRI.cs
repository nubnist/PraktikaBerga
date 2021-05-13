using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using DataCalc;

namespace WpfApp1.Data
{
    public class IRI
    {
        public CharacteristicIRI CharacteristicIri { get; set; }
        public List<CharacteristicStream> IriStream { get; set; }
        public int Number { get; set; }
    }
}
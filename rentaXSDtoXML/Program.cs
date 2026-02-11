using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace rentaXSDtoXML
{
    public class Program
    {
        private static XmlSchema esquemaXml; // Variable global o de clase
        static void Main(string[] argumentos)
        {
            string ficheroEntrada = argumentos[0];
            string ficheroSalida = Path.ChangeExtension(ficheroEntrada, "xml");
            string ficheroSalida2 = "rem90000.xml";
            try
            {

                // Cargar y compilar el schema
                using(FileStream fs = new FileStream(ficheroEntrada, FileMode.Open))
                {
                    esquemaXml = XmlSchema.Read(fs, null);
                }

                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(esquemaXml);
                schemaSet.Compile();

                // Crear el documento XML
                XmlDocument documentoXml = new XmlDocument();

                // Procesar el elemento raíz
                XmlSchemaElement elementoRaiz = esquemaXml.Elements.Values.OfType<XmlSchemaElement>().FirstOrDefault();
                if(elementoRaiz != null)
                {
                    // Crear el elemento raíz en el documento
                    XmlElement root = documentoXml.CreateElement(elementoRaiz.Name);
                    documentoXml.AppendChild(root);

                    // Procesar el contenido del elemento raíz
                    ProcessSchemaElement(elementoRaiz, root, documentoXml);
                }

                // Guardar el XML
                using(StreamWriter sw = new StreamWriter(ficheroSalida))
                using(CustomXmlWriter writer = new CustomXmlWriter(sw))
                {
                    documentoXml.DocumentElement.WriteTo(writer);
                }
                using(StreamWriter sw2 = new StreamWriter(ficheroSalida2))
                using(CustomXmlWriter writer = new CustomXmlWriter(sw2))
                {
                    documentoXml.DocumentElement.WriteTo(writer);
                }
            }
            catch(Exception ex)
            {
                string pathSalida = Path.ChangeExtension(ficheroSalida, "sal");
                Console.WriteLine($"Error al procesar el archivo: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

        }

        public class CustomXmlWriter : XmlTextWriter
        {
            //Clase personalizada para formatear y grabar el XML
            public CustomXmlWriter(TextWriter w) : base(w)
            {
                //Al grabar el XML se hace indentando con tabulaciones
                Formatting = Formatting.Indented;
                IndentChar = '\t';
                Indentation = 1;
            }
        }

        private static void ProcessComplexType(XmlSchemaComplexType complexType, XmlElement parentElement, XmlDocument xmlDoc)
        {
            //Procesado de tipos complejos del esquema (complexType)
            if(complexType.Particle != null)
            {
                ProcessParticle(complexType.Particle, parentElement, xmlDoc);
            }
        }

        private static void ProcessParticle(XmlSchemaParticle particle, XmlElement parentElement, XmlDocument xmlDoc)
        {
            //Procesado de los componentes del esquema
            switch(particle)
            {
                case XmlSchemaSequence sequence:
                    //Procesa los elementos que son 'sequence'
                    foreach(XmlSchemaObject item in sequence.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;

                case XmlSchemaChoice choice:
                    //Procesa los elementos que son 'choice'
                    foreach(XmlSchemaObject item in choice.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;

                case XmlSchemaAll all:
                    //Procesa todos los elementos 
                    foreach(XmlSchemaObject item in all.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;
            }
        }

        private static void ProcessSchemaObject(XmlSchemaObject schemaObject, XmlElement parentElement, XmlDocument xmlDoc)
        {
            //Procesa los objetos que se le pasan desde el metodo para procesar los componentes del esquema 'ProcessParticle'
            switch(schemaObject)
            {
                case XmlSchemaElement element:
                    ProcessSchemaElement(element, parentElement, xmlDoc);
                    break;

                case XmlSchemaSequence sequence:
                    ProcessParticle(sequence, parentElement, xmlDoc);
                    break;

                case XmlSchemaChoice choice:
                    ProcessParticle(choice, parentElement, xmlDoc);
                    break;

                case XmlSchemaAll all:
                    ProcessParticle(all, parentElement, xmlDoc);
                    break;
            }
        }

        private static void ProcessSchemaElement(XmlSchemaElement schemaElement, XmlElement parentElement, XmlDocument xmlDoc)
        {
            // Si el elemento padre es el documento raíz y el elemento actual es "Declaracion"
            // procesamos directamente su tipo sin crear un nuevo elemento
            var elementType = schemaElement.ElementSchemaType as XmlSchemaComplexType;

            //Con esto se evita que se creen dos nodos 'Declaracion'
            if(parentElement == xmlDoc.DocumentElement && schemaElement.Name == "Declaracion")
            {
                if(elementType != null)
                {
                    ProcessComplexType(elementType, parentElement, xmlDoc);
                }
                return;
            }

            //Se añade un nuevo elemento como hijo del 'parentElement'
            XmlElement newElement = xmlDoc.CreateElement(schemaElement.Name);
            parentElement.AppendChild(newElement);

            //Si el elemento no es nulo, se envia al metodo para procesar tipos complejos
            if(elementType != null)
            {
                ProcessComplexType(elementType, newElement, xmlDoc);
            }
            else
            {
                // Si es un tipo simple o no tiene tipo definido, se añade un espacio en blanco como contenido del elemento
                newElement.InnerText = " ";
            }
        }
    }

    public class CustomXmlWriter : XmlTextWriter
    {
        public CustomXmlWriter(TextWriter w) : base(w)
        {
            Formatting = Formatting.Indented;
            IndentChar = ' ';
            Indentation = 4;
        }

        public override void WriteEndElement()
        {
            if(WriteState == WriteState.Element)
            {
                // Si el elemento está vacío o solo tiene un espacio, escríbelo en una línea
                WriteString(" ");
                WriteFullEndElement();
            }
            else
            {
                base.WriteEndElement();
            }
        }
    }
}
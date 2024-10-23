using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace rentaXSDtoXML
{
    internal class Program
    {
        static void Main(string[] argumentos)
        {
            string ficheroXsd = argumentos[0];
            string ficheroSalida = Path.ChangeExtension(ficheroXsd, "xml");

            StringBuilder salidaXML = new StringBuilder();
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add("", ficheroXsd);

            // Iterar sobre todos los elementos del XSD
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    ProcessElement(element, salidaXML, 0); // 0 es el nivel de indentación inicial
                }
            }
            File.WriteAllText(ficheroSalida, salidaXML.ToString());
        }


        public static void ProcessElement(XmlSchemaElement element, StringBuilder sb, int indentLevel)
        {
            // Generar la indentación en base al nivel actual
            string indent = new string(' ', indentLevel * 4); // 4 espacios por nivel de indentación

            // Agregar la apertura de la etiqueta
            sb.AppendLine($"{indent}<{element.Name}>");

            // Procesar el esquema de tipo del elemento
            if (element.ElementSchemaType != null)
            {
                // Manejar el caso de un tipo complejo
                if (element.ElementSchemaType is XmlSchemaComplexType complexType)
                {
                    // Comprobar si tiene hijos en la secuencia
                    if (complexType.ContentTypeParticle is XmlSchemaSequence sequence)
                    {
                        foreach (XmlSchemaParticle particle in sequence.Items)
                        {
                            if (particle is XmlSchemaElement childElement)
                            {
                                // Recursión para procesar el elemento hijo
                                ProcessElement(childElement, sb, indentLevel + 1);
                            }
                        }
                    }
                }
                // Manejar el caso de un tipo simple
                else if (element.ElementSchemaType is XmlSchemaSimpleType)
                {
                    // Si el tipo es simple, agregar un elemento vacío o gestionar sus hijos si los tiene
                    sb.AppendLine($"{indent}    <{element.Name}></{element.Name}>");
                }
            }
            else
            {
                // Si no hay un tipo de elemento, agregar un elemento vacío
                sb.AppendLine($"{indent}    <{element.Name}></{element.Name}>");
            }

            // Cerrar la etiqueta del elemento
            sb.AppendLine($"{indent}</{element.Name}>");
        }

        public static void ProcessSchema(XmlSchemaSet schemaSet, StringBuilder sb)
        {
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    // Procesar cada elemento principal del XSD
                    ProcessElement(element, sb, 0); // 0 es el nivel de indentación inicial
                }
            }
        }




    }
}

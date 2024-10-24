using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace rentaXSDtoXML
{
    public class Program
    {
        private static XmlSchema schema; // Variable global o de clase
        static void Main(string[] argumentos)
        {
            try
            {
                // Asumiendo que tienes estas variables con los nombres de archivo
                string xsdPath = argumentos[0];
                string xmlOutputPath = Path.ChangeExtension(xsdPath, "xml");

                // Cargar y compilar el schema
                using (FileStream fs = new FileStream(xsdPath, FileMode.Open))
                {
                    schema = XmlSchema.Read(fs, null);
                }

                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(schema);
                schemaSet.Compile();

                // Crear el documento XML
                XmlDocument xmlDoc = new XmlDocument();
                //XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                //xmlDoc.AppendChild(xmlDeclaration);

                // Procesar el elemento raíz
                XmlSchemaElement rootElement = schema.Elements.Values.OfType<XmlSchemaElement>().FirstOrDefault();
                if (rootElement != null)
                {
                    // Crear el elemento raíz en el documento
                    XmlElement root = xmlDoc.CreateElement(rootElement.Name);
                    xmlDoc.AppendChild(root);

                    // Procesar el contenido del elemento raíz
                    ProcessSchemaElement(rootElement, root, xmlDoc);
                }

                // Guardar el XML usando el nuevo método SaveXml
                using (StreamWriter sw = new StreamWriter(xmlOutputPath))
                using (CustomXmlWriter writer = new CustomXmlWriter(sw))
                {
                    xmlDoc.DocumentElement.WriteTo(writer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar el archivo: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

        }

        private static void SaveXml(XmlDocument doc, string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            using (CustomXmlWriter writer = new CustomXmlWriter(sw))
            {
                doc.Save(writer);
            }
        }

        public class CustomXmlWriter : XmlTextWriter
        {
            public CustomXmlWriter(TextWriter w) : base(w)
            {
                Formatting = Formatting.Indented;
                IndentChar = '\t';
                Indentation = 1;
            }

            public override void WriteEndElement()
            {
                if (WriteState == WriteState.Element)
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


        public static XmlDocument GenerateXMLFromXSD(string xsdPath)
        {
            XmlSchema schema = LoadSchema(xsdPath);

            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add(schema);
            schemaSet.Compile();

            XmlDocument xmlDoc = new XmlDocument();

            // Crear el elemento raíz con sus atributos obligatorios
            XmlElement rootElement = xmlDoc.CreateElement("Declaracion");
            rootElement.SetAttribute("Modelo", "100");
            rootElement.SetAttribute("Ejercicio", "2023");
            rootElement.SetAttribute("Version", "1.0");
            xmlDoc.AppendChild(rootElement);

            // Procesar el esquema comenzando por el elemento raíz
            foreach (XmlSchemaElement element in schema.Elements.Values)
            {
                if (element.Name == "Declaracion")
                {
                    ProcessComplexType((XmlSchemaComplexType)element.SchemaType, rootElement, xmlDoc);
                }
            }

            return xmlDoc;
        }

        public static XmlSchema LoadSchema(string xsdPath)
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            XmlSchema schema;

            using (FileStream fs = new FileStream(xsdPath, FileMode.Open))
            {
                schema = XmlSchema.Read(fs, null);
                schemaSet.Add(schema);
            }

            try
            {
                // Validar y compilar el esquema
                schemaSet.Compile();

                // Manejar cualquier error de validación
                if (schemaSet.Count == 0)
                {
                    throw new Exception("El esquema XSD no se pudo cargar correctamente.");
                }

                // Agregar manejo de namespaces si es necesario
                foreach (XmlSchema s in schemaSet.Schemas())
                {
                    schema = s;
                    break;
                }
            }
            catch (XmlSchemaException ex)
            {
                throw new Exception($"Error al compilar el esquema XSD: {ex.Message}", ex);
            }

            return schema;
        }

        private static void ProcessComplexType(XmlSchemaComplexType complexType, XmlElement parentElement, XmlDocument xmlDoc)
        {
            if (complexType.Particle != null)
            {
                ProcessParticle(complexType.Particle, parentElement, xmlDoc);
            }
        }

        private static void ProcessParticle(XmlSchemaParticle particle, XmlElement parentElement, XmlDocument xmlDoc)
        {
            switch (particle)
            {
                case XmlSchemaSequence sequence:
                    foreach (XmlSchemaObject item in sequence.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;

                case XmlSchemaChoice choice:
                    foreach (XmlSchemaObject item in choice.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;

                case XmlSchemaAll all:
                    foreach (XmlSchemaObject item in all.Items)
                    {
                        ProcessSchemaObject(item, parentElement, xmlDoc);
                    }
                    break;
            }
        }

        private static void ProcessSchemaObject(XmlSchemaObject schemaObject, XmlElement parentElement, XmlDocument xmlDoc)
        {
            switch (schemaObject)
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
            if (parentElement == xmlDoc.DocumentElement && schemaElement.Name == "Declaracion")
            {
                if (elementType != null)
                {
                    ProcessComplexType(elementType, parentElement, xmlDoc);
                }
                return;
            }
            XmlElement newElement = xmlDoc.CreateElement(schemaElement.Name);
            parentElement.AppendChild(newElement);

            if (elementType != null)
            {
                ProcessComplexType(elementType, newElement, xmlDoc);
            }
            else
            {
                // Si es un tipo simple o no tiene tipo definido, solo añadir un espacio en blanco
                newElement.InnerText = " ";
            }
        }

        private static string GenerateDefaultValue(XmlSchemaSimpleType simpleType)
        {
            return " ";
        }

        private static string GenerateDefaultValueForBuiltInType(XmlSchemaElement element)
        {
            return " ";
        }

        private static string GenerateDefaultAttributeValue(XmlSchemaAttribute attribute)
        {
            return " ";
        }

        public static void ProcessElementReference(string referenceName, XmlElement element, XmlDocument xmlDoc)
        {
            // Este método debería manejar referencias a elementos globales
            // Por simplicidad, aquí solo establecemos un valor vacío
            element.InnerText = "";

            // En una implementación más completa, deberías:
            // 1. Buscar el elemento referenciado en el esquema
            // 2. Procesar ese elemento como corresponda
            // 3. Aplicar la misma lógica que para elementos normales
        }

        private static void ProcessChoice(XmlSchemaChoice choice, XmlElement parentElement, XmlDocument xmlDoc)
        {
            // Procesamos todos los elementos dentro del choice
            foreach (XmlSchemaObject item in choice.Items)
            {
                if (item is XmlSchemaElement element)
                {
                    ProcessSchemaElement(element, parentElement, xmlDoc);
                }
                else if (item is XmlSchemaSequence sequence)
                {
                    foreach (XmlSchemaObject seqItem in sequence.Items)
                    {
                        if (seqItem is XmlSchemaElement seqElement)
                        {
                            ProcessSchemaElement(seqElement, parentElement, xmlDoc);
                        }
                    }
                }
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
            if (WriteState == WriteState.Element)
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
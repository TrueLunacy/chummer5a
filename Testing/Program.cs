// See https://aka.ms/new-console-template for more information
using Chummer.Api;
using Chummer.Api.Models.Character;
using System.Collections.Immutable;
using System.Text;
using System.Xml;

FileInfo file = new FileInfo("C:/Users/Lunacy/Desktop/Aglaya.chum5");
using var reader = XmlReader.Create(file.OpenRead());

Character chara = Character.Read(reader);

;

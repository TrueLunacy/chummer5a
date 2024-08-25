using RecordSourceGenerator.Generated;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Chummer.Api.Models.Character
{
    public sealed partial class Bonus();
    public sealed partial class Gear();

    [XmlRecord(ElementName = "armormod")]
    public sealed partial record ArmourMod(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "suid")] Guid SourceGuid,
        string Name,
        string Category,
        int ArmourValue,
        int ArmourCapacity,
        int GearCapacity,
        int MaxRating,
        int Rating,
        string RatingLabel,
        string Availability,
        string Cost,
        string Weight,
        ImmutableArray<Gear> Gear,
        ImmutableArray<Bonus> Bonuses,
        ImmutableArray<Bonus> WirelessBonuses,
        bool WirelessOn,
        string Source,
        string Page,
        bool IncludedInArmour,
        bool Equipped,
        string Extra,
        bool Stolen,
        Guid WeaponGuid,
        string Notes,
        string NotesColour,
        bool DiscountedCost,
        int SortOrder
    );

    // [XmlAsElement(ElementName = "")]

    [XmlRecord(ElementName = "armor")]
    public sealed partial record Armour(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "suid")] Guid SourceGuid,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "category")] string Category,
        [XmlAsElement(ElementName = "armor")] string ArmourValue,
        [XmlAsElement(ElementName = "armoroverride")] string ArmourOverride,
        [XmlAsElement(ElementName = "armorcapacity")] string ArmourCapacity,
        [XmlAsElement(ElementName = "avail")] string Availability,
        [XmlAsElement(ElementName = "cost")] string Cost,
        [XmlAsElement(ElementName = "weight")] string Weight,
        [XmlAsElement(ElementName = "source")] string Source,
        [XmlAsElement(ElementName = "page")] string Page,
        [XmlAsElement(ElementName = "armorname")] string ArmourName,
        [XmlAsElement(ElementName = "equipped")] bool Equipped,
        [XmlAsElement(ElementName = "devicerating")] string DeviceRating,
        [XmlAsElement(ElementName = "programlimit")] string ProgramLimit,
        [XmlAsElement(ElementName = "overclocked")] string Overclocked,
        [XmlAsElement(ElementName = "attack")] string Attack,
        [XmlAsElement(ElementName = "sleaze")] string Sleaze,
        [XmlAsElement(ElementName = "dataprocessing")] string DataProcessing,
        [XmlAsElement(ElementName = "firewall")] string Firewall,
        [XmlAsElement(ElementName = "attributearray")] string AttributeArray,
        [XmlAsElement(ElementName = "modattack")] string ModAttack,
        [XmlAsElement(ElementName = "modsleaze")] string ModSleaze,
        [XmlAsElement(ElementName = "moddataprocessing")] string ModDataProcessing,
        [XmlAsElement(ElementName = "modfirewall")] string ModFirewall,
        [XmlAsElement(ElementName = "modattributearray")] string ModAttributeArray,
        [XmlAsElement(ElementName = "canswapattributes")] bool CanSwapAttributes,
        [XmlAsElement(ElementName = "matrixcmfilled")] int MatrixConditionMonitorFilled,
        [XmlAsElement(ElementName = "matrixcmbonus")] int MatrixConditionMonitorBonus,
        [XmlAsElement(ElementName = "wirelesson")] bool WirelessOn,
        [XmlAsElement(ElementName = "canformpersona")] string CanFormPersona,
        [XmlAsElement(ElementName = "extra")] string Extra,
        [XmlAsElement(ElementName = "damage")] int Damage,
        [XmlAsElement(ElementName = "rating")] int Rating,
        [XmlAsElement(ElementName = "maxrating")] int MaxRating,
        [XmlAsElement(ElementName = "ratinglabel")] string RatingLabel,
        [XmlAsElement(ElementName = "stolen")] bool Stolen,
        [XmlAsElement(ElementName = "emcumbrance")] string Encumbrance,
        [XmlAsElement("armormods", ElementName = "armormod")]
            ImmutableArray<ArmourMod> ArmourMods
    );

    [XmlRecord(ElementName = "location")]
    public sealed partial record Location(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "notes")] string Notes,
        [XmlAsElement(ElementName = "notesColor")] string NotesColour,
        [XmlAsElement(ElementName = "sortorder")] int SortOrder
    );

    [XmlRecord(ElementName = "spec")]
    public sealed partial record SkillSpec(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "free")] bool Free,
        [XmlAsElement(ElementName = "expertise")] bool Expertise
    );

    [XmlRecord(ElementName = "skill")]
    public sealed partial record Skill(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "suid")] Guid SourceGuid,
        [XmlAsElement(ElementName = "isknowledge")] bool IsKnowledge,
        [XmlAsElement(ElementName = "skillcategory")] string SkillCategory,
        [XmlAsElement(ElementName = "requiresgroundmovement")] bool RequiresGroundMovement,
        [XmlAsElement(ElementName = "requiresswimmovement")] bool RequiresSwimMovement,
        [XmlAsElement(ElementName = "requiresflymovement")] bool RequiresFlyMovement,
        [XmlAsElement(ElementName = "karma")] int Karma,
        [XmlAsElement(ElementName = "base")] int Base,
        [XmlAsElement(ElementName = "notes")] string Notes,
        [XmlAsElement(ElementName = "notesColor")] string NotesColour,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "specs", ArrayElementName = "spec")]
            ImmutableArray<SkillSpec> Specs
    );

    [XmlRecord(ElementName = "group")]
    public sealed partial record SkillGroup(
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "id")] Guid OurGuid,
        [XmlAsElement(ElementName = "isbroken")] bool IsBroken,
        [XmlAsElement(ElementName = "base")] int Base,
        [XmlAsElement(ElementName = "karma")] int Karma
    );

    [XmlRecord(ElementName = "newskills")]
    public sealed partial record NewSkills(
        [XmlAsElement(ElementName = "skillptsmax")] int MaxSkillPoints,
        [XmlAsElement(ElementName = "skillgrpsmax")] int MaxGroupPoints,
        [XmlAsElement(ElementName = "skills", ArrayElementName = "skill")]
            ImmutableArray<Skill> Skills,
        [XmlAsElement(ElementName = "knoskills", ArrayElementName = "skill")]
            ImmutableArray<Skill> KnowledgeSkills,
        [XmlAsElement(ElementName = "skilljackknowledgeskills", ArrayElementName = "skill")]
            ImmutableArray<Skill> SkilljackKnowledgeSkills,
        [XmlAsElement(ElementName = "groups", ArrayElementName = "group")]
            ImmutableArray<SkillGroup> SkillGroups
    );

    [XmlRecord(ElementName = "tradition")]
    public sealed partial record Tradition(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "sourceid")] Guid SourceGuid
    );

    [XmlRecord(ElementName = "attribute")]
    public sealed partial record Attribute(
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "metatypemin")] int MetatypeMinimum,
        [XmlAsElement(ElementName = "metatypemax")] int MetatypeMaximum,
        [XmlAsElement(ElementName = "metatypeaugmax")] int MetatypeAugMaximum,
        [XmlAsElement(ElementName = "base")] int Base,
        [XmlAsElement(ElementName = "karma")] int Karma
    );

    [XmlRecord(ElementName = "quality")]
    public sealed partial record Quality(
        [XmlAsElement(ElementName = "guid")] Guid OurGuid,
        [XmlAsElement(ElementName = "sourceid")] Guid SourceGuid 
    );

    [XmlRecord(ElementName = "mentorspirit")]
    public sealed partial record MentorSpirit(
        [XmlAsElement(ElementName = "guid")] Guid Guid,
        [XmlAsElement(ElementName = "sourceid")] Guid SourceGuid
    );

    [XmlRecord(ElementName = "improvement")]
    public sealed partial record Improvement(
        [XmlAsElement(ElementName = "unique")] string UniqueName,
        [XmlAsElement(ElementName = "target")] string Target,
        [XmlAsElement(ElementName = "improvedname")] string ImprovedName,
        [XmlAsElement(ElementName = "sourcename")] string SourceName,
        [XmlAsElement(ElementName = "min")] int Min,
        [XmlAsElement(ElementName = "max")] int Max,
        [XmlAsElement(ElementName = "aug")] decimal Aug,
        [XmlAsElement(ElementName = "augmax")] decimal AugMax,
        [XmlAsElement(ElementName = "val")] decimal Value,
        [XmlAsElement(ElementName = "exclude")] string Exclude,
        [XmlAsElement(ElementName = "condition")] string Condition,
        [XmlAsElement(ElementName = "custom")] bool Custom,
        [XmlAsElement(ElementName = "customname")] string CustomName,
        [XmlAsElement(ElementName = "customid")] string CustomId,
        [XmlAsElement(ElementName = "customgroup")] string CustomGroup,
        [XmlAsElement(ElementName = "addtorating")] int AddToRating,
        [XmlAsElement(ElementName = "enabled")] int Enabled,
        [XmlAsElement(ElementName = "notes")] string Notes,
        [XmlAsElement(ElementName = "notesColor")] string NotesColor,
        [XmlAsElement(ElementName = "order")] int Order,
        [XmlAsElement(ElementName = "improvementtype")] string ImprovementType,
        [XmlAsElement(ElementName = "improvementsource")] string ImprovementSource,
        [XmlAsElement(ElementName = "file")] string FileName
    );

    [XmlRecord(ElementName = "contact")]
    public sealed partial record Contact(
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "role")] string Role,
        [XmlAsElement(ElementName = "location")] string Location,
        [XmlAsElement(ElementName = "connection")] int Connection,
        [XmlAsElement(ElementName = "loyalty")] int Loyalty,
        [XmlAsElement(ElementName = "metatype")] string Metatype,
        [XmlAsElement(ElementName = "gender")] string Gender,
        [XmlAsElement(ElementName = "age")] string Age,
        [XmlAsElement(ElementName = "contacttype")] string Type,
        [XmlAsElement(ElementName = "preferredpayment")] string PreferredPayment,
        [XmlAsElement(ElementName = "hobbiesvice")] string HobbiesVice,
        [XmlAsElement(ElementName = "personallife")] string PersonalLife,
        [XmlAsElement(ElementName = "type")] string ContactType,
        [XmlAsElement(ElementName = "file")] string FileName,
        [XmlAsElement(ElementName = "notes")] string Notes,
        [XmlAsElement(ElementName = "notesColor")] string NotesColor,
        [XmlAsElement(ElementName = "groupname")] string GroyupName,
        [XmlAsElement(ElementName = "group")] bool IsGroup,
        [XmlAsElement(ElementName = "guid")] string Unique,
        [XmlAsElement(ElementName = "family")] bool Family,
        [XmlAsElement(ElementName = "blackmail")] bool Blackmail,
        [XmlAsElement(ElementName = "free")] bool Free,
        [XmlAsElement(ElementName = "colour")] int Colour,
        [XmlAsElement(ElementName = "readonly")] bool ReadOnly,
        [XmlAsElement(ElementName = "groupenabled")] bool GroupEnabled
    );

    [XmlRecord(ElementName = "character")]
    [XmlSpecialHandling("priorityskills")]
    public sealed partial record Character(
        [XmlAsElement(ElementName = "ignorerules")] bool IgnoreRules,
        [XmlAsElement(ElementName = "created")] bool Created,
        [XmlAsElement(ElementName = "gameedition")] string GameVersion,
        [XmlAsElement(ElementName = "appversion")] string AppVersion,
        [XmlAsElement(ElementName = "minimumappversion")] string MinimumAppVersion,
        // sometimes the .XML, sometimes a GUID
        [XmlAsElement(ElementName = "settings")] string SettingsFile,
        [XmlAsElement(ElementName = "settingshashcode")] int SettingsHashCode,
        [XmlAsElement(ElementName = "buildmethod")] string BuildMethod,
        [XmlAsElement(ElementName = "essenceatspecialstart")] decimal EssenceAtSpecialStart,
        [XmlAsElement(ElementName = "createdversion")] string CreatedVersion,
        [XmlAsElement(ElementName = "iscritter")] bool IsCritter,
        [XmlAsElement(ElementName = "metatype")] string Metatype,
        [XmlAsElement(ElementName = "metatypeid")] Guid MetatypeId,
        [XmlAsElement(ElementName = "movement")] string Movement,
        [XmlAsElement(ElementName = "walk")] string Walk,
        [XmlAsElement(ElementName = "run")] string Run,
        [XmlAsElement(ElementName = "sprint")] string Sprint,
        // todo: movement alt attributes
        [XmlAsElement(ElementName = "initiativedice")] int InitiativeDice,
        [XmlAsElement(ElementName = "metatypebp")] int MetatypeBp,
        [XmlAsElement(ElementName = "metavariant")] string Metavariant,
        [XmlAsElement(ElementName = "metavariantid")] Guid MetavariantId,
        [XmlAsElement(ElementName = "source")] string MetatypeBookSource,
        [XmlAsElement(ElementName = "page")] string MetatypeBookPage,
        [XmlAsElement(ElementName = "metatypecategory")] string MetatypeCategory,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "mainmugshotindex")] int MainMugshotIndex,
        [XmlAsElement(ElementName = "mugshots", ArrayElementName = "mugshot")]
            ImmutableArray<byte[]> Mugshots,
        [XmlAsElement(ElementName = "gender")] string Gender,
        [XmlAsElement(ElementName = "age")] string Age,
        [XmlAsElement(ElementName = "eyes")] string Eyes,
        [XmlAsElement(ElementName = "height")] string Height,
        [XmlAsElement(ElementName = "weight")] string Weight,
        [XmlAsElement(ElementName = "skin")] string Skin,
        [XmlAsElement(ElementName = "hair")] string Hair,
        [XmlAsElement(ElementName = "description")] string Description,
        [XmlAsElement(ElementName = "background")] string Background,
        [XmlAsElement(ElementName = "concept")] string Concept,
        [XmlAsElement(ElementName = "notes")] string Notes,
        [XmlAsElement(ElementName = "alias")] string Alias,
        [XmlAsElement(ElementName = "playername")] string PlayerName,
        [XmlAsElement(ElementName = "gamenotes")] string GameNotes,
        [XmlAsElement(ElementName = "primaryarm")] string PrimaryArm,
        [XmlAsElement(ElementName = "prioritymetatype")] string PriorityMetatype,
        [XmlAsElement(ElementName = "priorityattributes")] string PriorityAttributes,
        [XmlAsElement(ElementName = "priorityspecial")] string PrioritySpecial,
        [XmlNoParse] string PrioritySkills,
        [XmlAsElement(ElementName = "priorityresources")] string PriorityResources,
        [XmlAsElement(ElementName = "prioritytalent")] string PriorityTalent,
        [XmlNoParse] ImmutableArray<string> PrioritySkillAssignments,
        [XmlAsElement(ElementName = "possessed")] bool Possessed,
        [XmlAsElement(ElementName = "contactpoints")] int ContactPoints,
        [XmlAsElement(ElementName = "basecarrylimit")] decimal BaseCarryLimit,
        [XmlAsElement(ElementName = "baseliftlimit")] decimal BaseLiftLimit,
        [XmlAsElement(ElementName = "totalcarriedweight")] decimal TotalCarriedWeight,
        [XmlAsElement(ElementName = "encumbranceinterval")] decimal EncumbranceInterval,
        [XmlAsElement(ElementName = "cfplimit")] int ComplexFormLimit,
        [XmlAsElement(ElementName = "ainormalprogramlimit")] int AINormalProgramLimit,
        [XmlAsElement(ElementName = "aiadvancedprogramlimit")] int AIAdvancedProgramLimit,
        [XmlAsElement(ElementName = "currentcounterspellingdice")] int CurrentCounterspellingDice,
        [XmlAsElement(ElementName = "currentliftcarryhits")] int CurrentLiftCarryHits,
        [XmlAsElement(ElementName = "spellimit")] int FreeSpells,
        [XmlAsElement(ElementName = "karma")] int Karma,
        [XmlAsElement(ElementName = "totalkarma")] int TotalKarma,
        [XmlAsElement(ElementName = "special")] int Special,
        [XmlAsElement(ElementName = "totalspecial")] int TotalSpecial,
        [XmlAsElement(ElementName = "totalattributes")] int TotalAttributes,
        [XmlAsElement(ElementName = "edgeused")] int EdgeUsed,
        [XmlAsElement(ElementName = "streetcred")] int StreetCred,
        [XmlAsElement(ElementName = "notoriety")] int Notoriety,
        [XmlAsElement(ElementName = "publicawareness")] int PublicAwareness,
        [XmlAsElement(ElementName = "burntstreetcred")] int BurntStreetCred,
        [XmlAsElement(ElementName = "baseastrapreputation")] int BaseAstralRep,
        [XmlAsElement(ElementName = "basewildreputation")] int BaseWildReputation,
        [XmlAsElement(ElementName = "nuyen")] decimal Nuyen,
        [XmlAsElement(ElementName = "startingnuyen")] decimal StartingNuyen,
        [XmlAsElement(ElementName = "nuyenbp")] decimal NuyenBp,
        [XmlAsElement(ElementName = "adept")] bool IsAdept,
        [XmlAsElement(ElementName = "magician")] bool IsMagician,
        [XmlAsElement(ElementName = "technomancer")] bool IsTechnomancer,
        [XmlAsElement(ElementName = "ai")] bool IsAi,
        [XmlAsElement(ElementName = "cyberwaredisabled")] bool CyberwareDisabled,
        [XmlAsElement(ElementName = "initiationdisabled")] bool InitiationDisabled,
        [XmlAsElement(ElementName = "critter")] bool HasCritterPowers, // todo check
        [XmlAsElement(ElementName = "prototypetranshuman")] bool IsPrototypeTranshuman,
        [XmlAsElement(ElementName = "magenabled")] bool MagicAttributeEnabled,
        [XmlAsElement(ElementName = "initiategrade")] int InitiateGrade,
        [XmlAsElement(ElementName = "resenabled")] bool ResonanceAttributeEnabled,
        [XmlAsElement(ElementName = "submersiongrade")] int SubmersionGrade,
        [XmlAsElement(ElementName = "depenabled")] bool DepthAttributeEnabled,
        [XmlAsElement(ElementName = "groupmember")] bool MemberOfGroup,
        [XmlAsElement(ElementName = "groupname")] string NameOfGroup,
        [XmlAsElement(ElementName = "groupnotes")] string GroupNotes,
        [XmlAsElement(ElementName = "magsplitadept")] int MagSplitAdept,
        [XmlAsElement(ElementName = "magsplitmagician")] int MagSplitMagician,
        [XmlAsElement(ElementName = "physicalcmfilled")] int PhysicalConditionMonitorFilled,
        [XmlAsElement(ElementName = "stuncmfilled")] int StunConditionMonitorFilled,
        [XmlAsElement(ElementName = "psyche")] bool PsycheActive,
        [XmlAsElement(ElementName = "mentorspirits", ArrayElementName = "mentorspirit")]
            ImmutableArray<MentorSpirit> MentorSpirits,
        [XmlAsElement(ElementName = "improvements", ArrayElementName = "improvement")]
            ImmutableArray<Improvement> Improvements,
        [XmlAsElement(ElementName = "contacts", ArrayElementName = "contact")]
            ImmutableArray<Contact> Contacts,
        [XmlAsElement(ElementName = "attributes", ArrayElementName = "attribute")]
            ImmutableArray<Attribute> Attributes,
        [XmlAsElement(ElementName = "newskills")] NewSkills Skills,
        [XmlAsElement(ElementName = "gearlocations", ArrayElementName = "location")]
            ImmutableArray<Location> GearLocations,
        [XmlAsElement(ElementName = "armorlocations", ArrayElementName = "location")]
            ImmutableArray<Location> ArmourLocations,
        [XmlAsElement(ElementName = "vehiclelocations", ArrayElementName = "location")]
            ImmutableArray<Location> VehicleLocations,
        [XmlAsElement(ElementName = "weaponlocations", ArrayElementName = "location")]
            ImmutableArray<Location> WeaponLocations,
        [XmlAsElement(ElementName = "armors", ArrayElementName = "armor")]
            ImmutableArray<Armour> Armours,
    )
    {
        private static partial byte[] ParseMugshots(XmlReader reader, byte[]? defaultvalue)
        {
#warning todo this
            return null!;
            //throw new NotImplementedException();
        }

        private static partial void WriteMugshots(byte[] value, XmlWriter writer, string elementName)
        {
            throw new NotImplementedException();
        }

        private static partial void Parsepriorityskills(XmlReader reader, Character character)
        {
            XNode node = XNode.ReadFrom(reader);
            if (node.NodeType != XmlNodeType.Element)
                throw new NotImplementedException();
            XElement element = (XElement)node;
            var descendants = element.DescendantNodes().ToArray();
            if (descendants.OfType<XElement>().Any(d => d.Name == "priorityskill"))
            { // array case
                var nodes = descendants.OfType<XElement>()
                    .Select(d => d.Value)
                    .ToImmutableArray();
                MethodInfo minfo = typeof(Character)
                    .GetProperty(nameof(PrioritySkillAssignments))!
                    .GetSetMethod()!;
                minfo.Invoke(character, [nodes]);
            }
            else if (descendants.Any(d => d.NodeType == XmlNodeType.Text))
            {
                var str = descendants.OfType<XText>().Single().Value;
                MethodInfo minfo = typeof(Character)
                    .GetProperty(nameof(PrioritySkills))!
                    .GetSetMethod()!;
                minfo.Invoke(character, [str]);
            }
        }

        private partial void WriteUnknownValues(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }

}

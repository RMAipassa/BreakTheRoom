# Overdrachtsdocumentatie Break The Room

## Doel van het project
Break The Room is een Unity VR rage-room game waarin de speler met fysieke tools objecten kapot maakt. De gameplay is gebaseerd op chaos score, destructie, instortende constructies en correcte toolbewegingen. Het project is bedoeld als schoolproject en moet overdraagbaar zijn aan een volgende groep.

## Projectstatus
Het project bevat op dit moment een werkend technisch prototype met:
- Een gegenereerde VR-arena met destructible objecten.
- Een XR Interaction Toolkit rig voor VR.
- Desktop fallback controls voor testen zonder headset.
- Fysieke tools zoals bat, hammer, crowbar, sword en axe.
- VR equip/drop controls via rechter en linker trigger.
- Score/timer systeem via `ChaosGameManager`.
- Wrist-mounted HUD met rode scoretekst.
- Hit-face systeem waarbij tools alleen goed werken als de juiste kant van het wapen raakt.
- Wear OS koppeling voor hartslag/stappen als extra input.

## Belangrijke Unity-instellingen
- Unity versie gebruikt tijdens ontwikkeling: `6000.3.12f1`.
- XR runtime: OpenXR via XR Plugin Management.
- Doel: headset-onafhankelijk werken via OpenXR en XR Interaction Toolkit.
- Geteste/gebruikte headsets tijdens ontwikkeling:
  - Vive Cosmos via SteamVR/OpenXR.
  - Meta Quest 2 via OpenXR.
- Belangrijke packages:
  - XR Interaction Toolkit
  - Input System
  - XR Plugin Management
  - OpenXR Plugin

## Snel Opstarten
Open het project in Unity en gebruik de menu-opties onder `Tools > Break The Room`.

Aanbevolen rebuild-stap voor de huidige demo:
1. `Tools > Break The Room > Delete Generated World`
2. `Tools > Break The Room > Force Rebuild XRI + Teardown Arena`
3. Start Play Mode met Vive Cosmos/SteamVR actief.

Als er zonder headset getest moet worden:
1. `Tools > Break The Room > Add XR Device Simulator`
2. Bij action asset warnings: `Tools > Break The Room > Fix XR Device Simulator Action Assets`
3. Test met desktop controls in Play Mode.

## Controls
VR controls:
- Rechter trigger/R2: tool oppakken/equippen.
- Linker trigger/L2: tool laten vallen.
- Beide gripknoppen ongeveer 1 seconde vasthouden: tool calibration mode aan/uit.
- Tijdens calibration kunnen controllerassen gebruikt worden om positie/rotatie van het tool in de hand te tunen.

Desktop fallback controls:
- `WASD`: bewegen.
- `Left Shift`: sprinten.
- Muis: kijken.
- Linkermuisknop: debug strike.
- Rechtermuisknop: arm/tool swing.
- `F`: tool oppakken.
- `G`: tool laten vallen.
- `1` / `2`: holster wisselen.
- `Q` / `E`: hand pose test.
- `Esc`: cursor unlock.

## Huidige VR Tool Calibration
De laatst gebruikte tool calibration is vooral getuned op een Vive Cosmos en staat in `XrDesktopToolInteractor` / gegenereerde tool setup:
- `vrMountPositionOffset = (0.03, -0.05, 0.10)`
- `vrMountEulerOffset = (34.08, -72.69, 36.69)`

Deze waarden zijn handmatig getuned in VR. Omdat het project headset-onafhankelijk bedoeld is, moeten deze waarden bij andere controllers/headsets waarschijnlijk opnieuw worden ingesteld.

## Architectuur
Belangrijkste systemen:

- `Assets/Scripts/Core/ChaosGameManager.cs`
  - Beheert score, timer en game state.

- `Assets/Scripts/Destruction/BreakablePiece.cs`
  - Health, damage, breken van objecten, fracture prefab spawning en impact propagation.

- `Assets/Scripts/Destruction/ImpactDamageDealer.cs`
  - Zet botsingsenergie om naar damage.

- `Assets/Scripts/Destruction/StructuralLink.cs`
  - Gebruikt joints om constructies te laten breken/instorten.

- `Assets/Scripts/Destruction/DestructionFeedback.cs`
  - Particles/audio per materiaaltype.

- `Assets/Scripts/Gameplay/ScoreOnBreak.cs`
  - Koppelt kapotte objecten aan chaos score.

- `Assets/Scripts/Gameplay/ChaosHudOverlay.cs`
  - Wrist-mounted score HUD.

- `Assets/Scripts/Player/XrDesktopToolInteractor.cs`
  - VR en desktop equip/drop, swing detection, tool calibration en damage application.

- `Assets/Scripts/Player/DesktopMeleeTool.cs`
  - Data per tool: damage, radius, impulse, hold offsets en hit-face profile reference.

- `Assets/Scripts/Combat/ToolHitFaceProfile.cs`
  - ScriptableObject met hit zones per tool.

- `Assets/Scripts/Combat/ToolHitFaceEvaluator.cs`
  - Runtime validatie van correcte hit face, snelheid en richting.

- `Assets/Scripts/Editor/ChaosWorldBuilder.cs`
  - Bouwt starter world, XRI rig, tools en generated assets.

- `Assets/Scripts/Editor/TeardownArenaBuilder.cs`
  - Bouwt de huidige Teardown-style arena.

- `Assets/Scripts/Integration/WearHealthUdpReceiver.cs`
  - Ontvangt Wear OS data via UDP.

- `Assets/Scripts/Integration/WearHealthChaosBridge.cs`
  - Zet wearable input om naar chaos score.

## Destructie
Objecten die kapot moeten kunnen hebben meestal:
- `Rigidbody`
- Collider
- `BreakablePiece`
- Eventueel `ScoreOnBreak`
- Eventueel `ImpactDamageDealer`

Het systeem gebruikt geen runtime mesh slicing. In plaats daarvan worden objecten vervangen door fracture prefabs of gegenereerde chunks. Dit is bewuster gekozen omdat VR performance belangrijk is.

Materiaaltypes hebben durability modifiers in `BreakablePiece`:
- Wood: `0.85`
- Glass: `1.25`
- Concrete: `0.70`
- Metal: `0.65`
- Generic: `1.0`

Let op: in dit systeem betekent een hogere modifier dat damage effectiever is. Glas breekt dus makkelijker dan beton/metaal.

## Hit-Face Systeem
Het hit-face systeem zorgt dat een tool niet zomaar met elke collider damage doet. Een hammer moet bijvoorbeeld met de hammer head raken, een sword met de edge/tip en een axe met blade/poll.

Documentatie hierover staat in:
- `Docs/Tool_Hit_Face_Design.md`
- `Docs/Tool_Hit_Face_Technical_Plan.md`

Generated profiles staan onder:
- `Assets/Generated/ToolProfiles/`

Belangrijk: als tools opnieuw worden gegenereerd via de builder, worden ook profiles en tool references opnieuw opgezet. Tune daarom bij voorkeur de builder/profiles en niet alleen losse scene-instances.

## Scene Generatie
Veel content wordt via editor scripts gegenereerd. Dit is handig voor overdracht omdat de scene opnieuw opgebouwd kan worden.

Belangrijke menu-opties:
- `Tools > Break The Room > Create New Scene and Build Starter World`
- `Tools > Break The Room > Build Starter World In Active Scene`
- `Tools > Break The Room > Delete Generated World`
- `Tools > Break The Room > Force Rebuild With XRI Rig`
- `Tools > Break The Room > Build Teardown-Style Arena`
- `Tools > Break The Room > Force Rebuild XRI + Teardown Arena`
- `Tools > Break The Room > Add XR Device Simulator`
- `Tools > Break The Room > Fix XR Device Simulator Action Assets`

Aanbeveling voor de volgende groep: gebruik de editor builders als bron van waarheid voor de demo-scene. Handmatige wijzigingen in de gegenereerde scene kunnen verdwijnen na rebuild.

## Bekende Issues en Risico's
- De exacte hand/tool offset is vooral getuned op Vive Cosmos en kan per headset/controller verschillen. Test dit ook opnieuw op Meta Quest 2 of andere headsets.
- De wrist HUD positie moet mogelijk nog in VR worden bijgesteld.
- Hit-face tuning is functioneel, maar gameplay-balans moet nog getest worden.
- Er bestaat mogelijk dubbele scoring:
  - `BreakablePiece.Break()` kan nog naar de oude `VRScoreBoard` schrijven.
  - `ScoreOnBreak` schrijft naar `ChaosGameManager`.
- `Assets/Scripts/VRScoreboard.CS` bestaat nog als oud scoreboard script en is mogelijk overbodig.
- Generated scene content kan handmatige aanpassingen overschrijven.
- Wear OS integratie is aanwezig, maar moet op hetzelfde netwerk en met de juiste poort getest worden.
- De huidige watch-integratie werkt technisch, maar voelt nog niet fijn genoeg voor gameplay. Dit moet verder ontworpen, getest en gepolijst worden.

## Aanbevolen Vervolgwerk
Technisch:
- Controleer en verwijder dubbele scoring via `VRScoreBoard` als deze niet meer nodig is.
- Maak VR calibration persistent, bijvoorbeeld via PlayerPrefs of een config asset.
- Voeg een in-game debug overlay toe voor afgekeurde hit-face hits.
- Tune performance bij veel debris en grote collapses.
- Maak betere tool prefabs/modellen in plaats van primitive generated geometry.
- Implementeer dual wielding, zodat tools ook met links gebruikt kunnen worden en linkshandige spelers goed ondersteund worden.

Gameplay:
- Balans van damage, score en timer testen met echte spelers.
- Meer duidelijke doelen toevoegen, bijvoorbeeld target value of missies.
- Betere hit feedback toevoegen, zodat spelers duidelijker merken of een hit goed, fout, zwak of krachtig was.
- Combo scoring toevoegen voor accurate swings.
- Tool progression of economy systeem toevoegen.
- Asset uitbreiding: betere looks, meer breekbare items en sterkere visuals voor de arena/tools.
- Beter sound design: harde hits moeten duidelijk harder/intensiever klinken dan zachte hits.
- Betere watch-integratie: duidelijker maken wat hartslag/stappen doen, stabielere koppeling maken en zorgen dat het prettig voelt tijdens gameplay.

Documentatie:
- Houd deze overdrachtsdocumentatie bij na grote wijzigingen.
- Voeg screenshots toe van de scene, tools en HUD zodra de demo stabiel is.
- Documenteer definitieve build-instructies voor de eindoplevering.

## Belangrijke Bestanden
- `README.md`: korte setup en controls.
- `Docs/Overdrachtsdocumentatie.md`: dit document.
- `Docs/Tool_Hit_Face_Design.md`: ontwerp van hit faces.
- `Docs/Tool_Hit_Face_Technical_Plan.md`: technische implementatie van hit faces.
- `Assets/Scripts/Editor/ChaosWorldBuilder.cs`: belangrijkste builder.
- `Assets/Scripts/Editor/TeardownArenaBuilder.cs`: arena builder.
- `Assets/Scripts/Player/XrDesktopToolInteractor.cs`: belangrijkste VR/tool interaction script.
- `Assets/Scripts/Destruction/BreakablePiece.cs`: belangrijkste destructie script.
- `Assets/Scripts/Core/ChaosGameManager.cs`: score/timer/game state.

## Overdrachtsadvies
De volgende groep kan het beste starten met drie stappen:
1. Lees `README.md` en deze overdrachtsdocumentatie.
2. Rebuild de demo via `Force Rebuild XRI + Teardown Arena`.
3. Test direct in VR, want veel belangrijke problemen zijn alleen in de headset goed zichtbaar.

# Jednodňový PoC Plán: Od Concept Artu po Gnolla, ktorý ťa naháňa v lese (Gothic / Retro Štýl)

## 1. Máš teraz experimentovať? A môžeš to neskôr meniť?
**Áno, TERAZ (v PoC fáze) je jediný správny čas na experimentovanie.**
* **Prečo teraz:** Cieľom dnešného dňa je nájsť tzv. **"Golden Pipeline"** – presný recept (prompt + 3D tool + Unity shader), ktorý ti za 20 minút a 0 € vyprodukuje hotového nepriateľa.
* **Môžeš to meniť počas vývoja?**
  * **Vizuálny štýl / Shader / Saturáciu:** ÁNO, kedykoľvek. Keďže v Unity použiješ globálny Post-Processing (alebo zdieľaný materiál), ak v 4. mesiaci zmeníš saturáciu, rozlíšenie pixelov alebo hmlu, zmení sa celá hra naraz za 10 sekúnd.
  * **Kostru (Rig) a mierku modelov:** NIE, toto zafixuj dnes! Všetci humanoidní nepriatelia musia ísť cez **Adobe Mixamo** s rovnakou kostrou. Vďaka tomu môžeš v Unity použiť jeden `Animator Controller` pre 10 rôznych príšer (Gnoll, Kostlivec, Rytier) a len im vymeníš 3D model.

---

## 2. Unifikovaná "Nulovo-Nákladová" Pipeline (Krok za krokom)

### Krok A: Midjourney (Štandardizovaná šablóna pre Gothic 2 / Dark Souls 1 štýl)
Tajomstvo Gothic 2 a Dark Souls 1 modelov je: **hladká, jednoduchá silueta (žiadne trčiace chlpy v 3D), pričom všetky detaily (srsť, svaly, nity) sú len namaľované na textúre.** Ak sú v Midjourney trčiace chlpy, AI sa ich snaží vymodelovať ako tisíce polygónov.

> **Prompt šablóna (Gothic 2 / PS2 / DS1 štýl):**
> `character sheet front view, full body A-pose of a [gnoll warrior wearing dark iron cuirass], Gothic 2 2002 PC game 3D model, low-poly smooth blocky geometry, hand-painted gritty texture details, painted-on fur texture, clean sharp silhouette, no protruding hair, standing straight, legs separated, feet clearly visible, symmetrical, flat even lighting, isolated on solid pure white background --ar 3:4 --stylize 50 --no weapon, shield, shadow, floor, ground, pedestal, cropped feet, cape, fluffy fur, spikes`

**Prečo to spraví obrovský rozdiel:**
* `smooth blocky geometry` + `painted-on fur texture` + `clean sharp silhouette`: Donúti Midjourney nakresliť postavu ako z roku 2002 (hranaté svaly, chlpy sú len textúra na hladkom tele).
* `--no fluffy fur, spikes`: Zabráni 3D generátoru robiť "ježka" z polygónov.

### Krok B: 30-sekundová príprava obrázka (kritický krok proti dieram a orezaným nohám!)
Predtým, než obrázok hodíš do 3D generátora:
1. Otvor obrázok v **Skicári (Paint)** alebo **Photopea.com**.
2. Ak sú chodidlá alebo ruky blízko okraja, **zväčši plátno (padding)** o ~15 % zo všetkých strán. Gnoll musí "plávať" v strede priestoru.
3. **POZOR na biele diery v TRELLISe:** Ak má Gnoll biele zuby, biele oči alebo bledú srsť, automatický odstraňovač pozadia v TRELLISe si môže myslieť, že je to biele pozadie, a **vypáli do modelu dieru (spolu s chýbajúcou textúrou)**.
   * *Prevencia:* Ak vidíš v náhľade TRELLISu po nahratí obrázka priesvitné diery v tele, vyfarbi v Skicári/Photopea biele pozadie okolo Gnolla plechovkou na **neutrálnu sivú (`#808080`) alebo krikľavú zelenú (`#00FF00`)**.

### Krok C: 100 % Zadarmo 2D-to-3D + "Gothic" Low-Poly Konverzia v Blenderi (Oprava dier a zachovanie textúry)
Keďže Tripo3D Web pýta peniaze za stiahnutie a Hunyuan robí priveľa polygónov, tu je **100 % bezplatné riešenie**, ktoré z každého AI modelu spraví ostrý Gothic 2 / DS1 model bez rozbitia textúry:

1. **Kde generovať 100 % ZADARMO (bez platenia za Download):**
   * **Možnosť 1 (Microsoft TRELLIS na HuggingFace Spaces):** Vyhľadaj na HuggingFace `TRELLIS` (od Microsoftu). Robí oveľa čistejšiu a hladšiu geometriu + ostrejšiu textúru než Hunyuan a sťahovanie `.glb` je 100 % zadarmo.
   * **Možnosť 2 (Hyper3D Rodin / Meshy Free / Hunyuan3D-2 na HuggingFace):** Stiahni `.glb` zadarmo.
2. **Magický 30-sekundový krok v Blenderi (Oprava dier + Z 80 000 polygónov na 2 500 so zachovaním UV textúry):**
   AI modely majú často nezvarené trojuholníky. Ak dáš `Decimate` priamo, vzniknú diery a roztrhne sa textúra. Sprav toto presné poradie:
   * Otvor Blender -> vymaž kocku -> `File -> Import -> glTF 2.0 (.glb)`.
   * *(Voliteľné: Hore vpravo v 3D okne prepni guľôčku na **Viewport Shading: Material Preview**, aby si na modeli rovno videl textúru).*
   * **Krok 2a – Zvarenie bodov (zabráni trhaniu modelu pri Decimate):**
     * Klikni na model Gnolla a stlač **`Tab`** (prepneš sa do *Edit Mode*).
     * Stlač **`A`** (označíš všetky body).
     * Stlač **`M`** -> vyber **By Distance** (zvarí duplicitné body bez pokazenia UV mapy).
     * Stlač **`Shift + N`** (otočí všetky normály von, aby model nemal "neviditeľné" steny).
     * Stlač **`Tab`** (späť do *Object Mode*).
   * **Krok 2b – Zníženie polygónov so zachovaním UV švov textúry (`Delimit: UVs`):**
     * Vpravo klikni na ikonku **francúzskeho kľúča (Modifiers)** -> `Add Modifier -> Generate -> Decimate`.
     * V **Decimate** nechaj zapnuté `Collapse`, nastav **Ratio** na **`0.06` až `0.1`** (cca `2 500 – 4 000 Faces`).
     * **KRITICKÉ PRE TEXTÚRU:** Hneď pod Ratio v modifikátore Decimate nájdi položku **Delimit** a klikni na tlačidlo **`UVs`**! Toto prikáže Blenderu, aby pri znižovaní polygónov **nikdy nedeformoval okraje textúry (UV seams)**.
   * *Voliteľné pre extra Gothic hranatosť:* Klikni pravým na model -> **Shade Flat** (namiesto Shade Smooth).
   * Klikni na šípku pri názve modifikátora Decimate -> **Apply**.
3. **Export do `.fbx` pre Mixamo:**
   * `File -> Export -> FBX (.fbx)`.
   * Vpravo hore nastav `Path Mode: Copy` a zaklikni ikonku krabičky vedľa (`Embed Textures`).

### Krok D: Adobe Mixamo (Rigging & Animácie zadarmo)
1. Choď na [mixamo.com](https://www.mixamo.com) (prihlásenie cez free Adobe účet).
2. Klikni **Upload Character** a nahraj svoj `.fbx`.
3. Umiestni krúžky: Brada (Chin), Zápästia (Wrists), Lakte (Elbows), Kolená (Knees), Rozkrok (Groin).
   * *Tip pre Gothic low-poly:* V rozbaľovacom menu **Skeleton LOD** vyber `No Fingers (25)` – ušetrí to výkon a pri low-poly pästiach prsty nepotrebuješ.
4. Stiahni si **1x samotnú postavu** v T-póze (`Download -> Format: FBX for Unity, Pose: T-Pose`) -> pomenuj `Gnoll_Model.fbx`.
5. Vyhľadaj v Mixame animácie a stiahni si ich **BEZ KOŽE (Without Skin)**:
   * Vyhľadaj `Zombie Idle` (alebo `Warrior Idle`) -> `Download -> Skin: Without Skin, FBX for Unity` -> pomenuj `Anim_Idle.fbx`.
   * Vyhľadaj `Zombie Run` (alebo `Run`) -> `Download (Without Skin)` -> pomenuj `Anim_Run.fbx`.
   * Vyhľadaj `Zombie Attack` (alebo `Mutant Swiping`) -> `Download (Without Skin)` -> pomenuj `Anim_Attack.fbx`.

---

## 3. Unity Setup: Ako si prezerať animácie a postaviť PoC Les

### Krok 1: Import Gnolla a prezeranie animácií v Unity
1. Vytvor si v Unity priečinok `Assets/PoC/gnoll` a presuň tam `gnoll.fbx`, `sword and shield idle.fbx`, `sword and shield run.fbx` a `sword and shield attack.fbx`.
2. **Nastavenie Humanoid kostry (na VŠETKÝCH súboroch):**
   * Označ všetky `.fbx` súbory v Project okne (alebo použi menu `PoC -> 1. Gnoll anims -> Humanoid`).
   * V **Inspector** okne (vpravo) prejni na záložku **Rig**.
   * Zmeň `Animation Type` z *Generic* na **Humanoid** a klikni **Apply**.
3. **Vybalenie textúry a oprava "priesvitných miest" v Unity:**
   * Klikni na `gnoll.fbx` -> záložka **Materials** -> klikni na **Extract Textures...** a **Extract Materials...** do toho istého priečinka.
   * Klikni na vyextrahovanú textúru a v Inspectore jej nastav:
     * `Filter Mode`: **Point (no filter)** (dá tomu ostrý pixel/retro vzhľad).
     * `Compression`: **None** -> klikni **Apply**.
   * Klikni na vyextrahovaný **Materiál** Gnolla a nastav:
     * `Render Face`: **Both** (Toto okamžite zakryje prípadné otočené polygóny, cez ktoré bolo vidieť dnu!).
     * `Smoothness`: **`0`** (Aby sa zapečené svetlo z TRELLISu nelesklo ako mokrý plast, ale vyzeralo ako matná textúra z Gothicu).
     * Zvýš sýtosť farby (Base Map tint) alebo zmeň shader na `Universal Render Pipeline/Simple Lit`.
4. **Ako Gnollovi neskôr pripnúť Zbraň a Štít (vždy generované zvlášť!):**
   * Zbraň a štít vygeneruj samostatne (bez postavy).
   * V Unity v Hierarchy rozklikni kostru Gnolla (`mixamorig:Hips` -> `Spine` -> ...):
     * 3D model **Sekery/Meča** pretiahni myšou priamo do kosti **`RightHand`**.
     * 3D model **Štítu** pretiahni myšou do kosti **`LeftForeArm`** (ľavé predlaktie).
5. **Ako si pozrieť animácie na tvojom Gnollovi priamo v Unity:**
   * Klikni na `sword and shield run.fbx` -> v Inspectore choď na záložku **Animation**.
   * Zaklikni **Loop Time** (aby animácia bežala dookola) a klikni **Apply**. (To isté sprav pre `sword and shield idle.fbx`).
   * Dole v Inspectore vidíš malé okno s náhľadom (Preview). **Potiahni myšou `gnoll.fbx` z Project okna priamo do tohto malého Preview okna dole vpravo!**
   * Stlač Play v malom Preview okne – teraz vidíš svojho Gnolla bežať a môžeš si ním otáčať!

---

### Krok 2: Prepojenie animácií (Animator Controller)
1. V priečinku `Assets/PoC/gnoll` klikni pravým tlačidlom -> `Create -> Animation -> Animator Controller` a nazvi ho `AC_Gnoll`.
2. Dvojklikom ho otvor (otvorí sa okno **Animator**).
3. Rozklikni šípkou `sword and shield idle.fbx`, `sword and shield run.fbx` a `sword and shield attack.fbx` v Project okne – vo vnútri uvidíš položku s názvom `mixamo.com` (samotný klip; ignoruj `mixamorig:Hips` a `... avatar`) a pretiahni tieto `mixamo.com` položky do okna Animator.
4. Na `Idle` klikni pravým -> `Set as Layer Default State` (bude oranžový).
5. Vľavo hore v okne Animator prepni na záložku **Parameters**, klikni na `+` -> **Bool** a nazvi ho `IsMoving`. Klikni znova na `+` -> **Trigger** a nazvi ho `Attack`.
6. Klikni pravým na `Idle` -> `Make Transition` -> potiahni šípku na `Run`.
   * Klikni na šípku, v Inspectore **odškrtni** `Has Exit Time` a dole v **Conditions** pridaj `IsMoving = true`.
7. Sprav opačnú šípku z `Run` do `Idle` -> odškrtni `Has Exit Time`, Condition: `IsMoving = false`.
8. Sprav šípku z `Any State` do `Attack` -> odškrtni `Has Exit Time`, Condition: `Attack`. A zo stavu `Attack` sprav šípku späť do `Idle` (tu **nechaj** `Has Exit Time` zaškrtnuté, aby útok dokončil).

---

### Krok 3: Basic "Gothic" Les za 10 minút
1. **Podlaha:** V Hierarchy klikni pravým -> `3D Object -> Plane`. Nastav `Scale` na `(10, 1, 10)`. Hoď na ňu tmavozelený/hnedý materiál.
2. **Stromy (Rýchly prototyp):**
   * Môžeš si v Midjourney vygenerovať 1 strom alebo použiť jednoduché valce (`3D Object -> Cylinder`, Scale `0.8, 6, 0.8`, tmavohnedá farba) – rozkopíruj ich cez `Ctrl+D` po lese (cca 20–30 kmeňov).
3. **Gothic Atmosféra (Hmla a Svetlo):**
   * Otvor `Window -> Rendering -> Lighting` -> záložka **Environment**.
   * Zaškrtni **Fog** (Hmla), nastav farbu na temnú sivo-zelenú/modrú, `Mode: Exponential Squared` a `Density: 0.04`.
   * Klikni v scéne na `Directional Light`, nastav mu jemne chladnú/mesačnú farbu a zníž `Intensity` na `0.5`. Okamžite to chytí atmosféru Khorinisu/Kolónie z Gothicu!
4. **NavMesh (Aby Gnoll vedel chodiť okolo stromov):**
   * Klikni na podlahu (`Plane`), pridaj jej komponent `NavMeshSurface` (ak ho nemáš, v `Window -> Package Manager -> Unity Registry` nainštaluj **AI Navigation**).
   * Na `NavMeshSurface` klikni na tlačidlo **Bake**. Podlaha sa zafarbí na modro – tadiaľto vie Gnoll chodiť.

---

### Krok 4: First-Person Hráč + Skript pre Gnolla (100% Funkčný Kód)

#### 1. Jednoduchý First-Person Hráč
1. V Hierarchy vytvor `3D Object -> Capsule`, nazvi ho `Player` a nastav mu tag hore v Inspectore na **Player**.
2. Pridaj mu komponent `Character Controller` a nastav `Height` na `1.8` a `Center` na `(0, 0.9, 0)`. Pozíciu `Player` nechaj na `Y = 0`.
3. Presuň `Main Camera` **do vnútra** objektu `Player` (ako dieťa) a nastav jej lokálnu pozíciu na `(0, 1.6, 0)`. Pôvodnú `Main Camera` v koreni Hierarchy vymaž (má druhý AudioListener a skript by mohol používať zlú kameru).
4. Vytvor skript `SimpleFPController.cs` a pripni ho na `Player`:

*(Nezabudni v Inspectore na `Player` pretiahnuť `Main Camera` do políčka `Camera Transform`!)*

#### 2. Zloženie Gnolla v scéne (vrátane 12-FPS Retro Efektu!)
1. Pretiahni `gnoll.fbx` (z priečinka `Assets/PoC/gnoll`) do scény na modrú podlahu.
2. V jeho komponente **Animator** priraď do políčka `Controller` svoj `AC_Gnoll`.
3. Pridaj na Gnolla komponent **Nav Mesh Agent** (nastav `Speed` napr. na `3.5` a `Stopping Distance` na `2.0`).
4. Pripni existujúci skript `Assets/PoC/Scripts/PoCGnollAI.cs` na Gnolla: pretiahni ho z Project okna na `gnoll` v Hierarchy, alebo v Inspectore klikni **Add Component** a napíš `PoCGnollAI`.

---

## 5. Upgrade scény na atmosféru z referenčného obrázka (Cintorín, Hrad, FP Meč, Pixel Filter)

Aby tvoja scéna vyzerala presne ako na referenčnom obrázku (zelenkavá mesačná hmla, silueta hradu v diaľke, kríže na cintoríne, ostré pixelové hrany a meč v pravej ruke), postupuj podľa týchto 5 krokov:

### Krok A: Zelenkavá nočná atmosféra, hmla a mesiac
1. **Oprava presvetlenia z Blenderu:** Ak máš v scéne na `gnoll` (alebo inom modeli) importované objekty `Light`, `Sun`, `Camera`, vymaž ich (alebo v `gnoll.fbx` -> záložka **Scene** odškrtni `Import Cameras` a `Import Lights` -> **Apply**).
2. **Hmla (Fog):** Otvor `Window -> Rendering -> Lighting -> Environment`:
   * Zaškrtni **Fog**, `Mode`: **Exponential Squared**, `Density`: **`0.035`**.
   * `Fog Color`: tmavá sivo-zelená (`#2A4032`).
3. **Pozadie oblohy (namiesto modrej):**
   * Klikni na `Main Camera` (vo vnútri `Player`).
   * Zmeň `Clear Flags` (alebo `Background Type` v URP) zo *Skybox* na **Solid Color** a nastav rovnakú tmavozelenú farbu ako má hmla (`#2A4032`). Vďaka tomu sa obzor plynulo stratí v hmle.
4. **Mesačné svetlo a samotný Mesiac:**
   * Klikni na `Directional Light`, nastav farbu na bledo-zelenú (`#B8E0C2`) a `Intensity` na **`0.6`**.
   * **Viditeľný Mesiac na oblohe:** Vytvor `3D Object -> Sphere`, nazvi ho `Moon`, daj ho ďaleko a vysoko (napr. `Position: (0, 60, 120)`, `Scale: (8, 8, 8)`), zmaž z neho `Sphere Collider` a daj naň čisto biely svietiaci materiál (`Unlit/Color` alebo `Emission` nabielo).

### Krok B: Cintorín (Kríže), Silueta Hradu a Les
1. **Kríže na cintoríne (bez modelovania, len z 2 kociek):**
   * Vytvor prázdny objekt `Cross`. Do neho daj dve kocky (`3D Object -> Cube`):
     * Zvislá časť: `Position (0, 1, 0)`, `Scale (0.25, 2.0, 0.25)`.
     * Vodorovná časť: `Position (0, 1.4, 0)`, `Scale (1.1, 0.22, 0.25)`.
   * Priraď im tmavosivý matný materiál (`Smoothness = 0`) a rozkopíruj (`Ctrl+D`) 20–30 krížov medzi stromy s jemným náhodným natočením.
2. **Silueta hradu na obzore (Low-Poly kulisa):**
   * Hrad v diaľke nemusí byť detailný model, pretože ho prekrýva hmla – stačí **čistá silueta**.
   * Poskladaj ho vo vzdialenosti `Z = 90` z niekoľkých veľkých kociek a valcov (`Cube` pre hradby a hranatú vežu, `Cylinder` + `Cone` alebo ihlan pre špicatú vežu, niekoľko malých kociek hore ako cimburie).
3. **Stromy s korunou a 3D tráva:**
   * Na existujúce kmene stromov (`Cylinder`) pridaj hore 2–3 tmavozelené zrezané gule/ihlan (`Icosphere` z Blenderu so `Shade Flat`, prípadne `Sphere` v Unity).
   * Pre hustú trávu na zemi použi Unity **Terrain** (štetec s detail meshom trávy) alebo rozmiestni krížom preložené placky (`Quad`) s pixel-art textúrou trávy a `Alpha Clipping`.

### Krok C: First-Person Ruka s Mečom (Viewmodel)
1. **Ruka a meč pripnuté na kameru:**
   * V Hierarchy rozbaľ `Player -> Main Camera`.
   * Klikni pravým na `Main Camera` -> `Create Empty`, nazvi ho `WeaponHolder` a nastav mu pozíciu **`(0.35, -0.3, 0.6)`** a rotáciu **`(10, -15, 0)`** (vpravo dole pred očami hráča).
2. **Rýchly prototyp meča (alebo import z Blenderu/TRELLISu):**
   * Do `WeaponHolder` vlož model meča (alebo poskladaj rýchly prototyp z tenkých kociek: čepeľ `Scale (0.04, 0.9, 0.08)`, záštita `Scale (0.35, 0.04, 0.06)`, rukoväť `Scale (0.05, 0.25, 0.05)` + hranatá ruka z hnedých kvádrov).
   * Na čepeľ daj tmavý kovový materiál s jemným odleskom hrán, aby sa na nej lámalo mesačné svetlo.
   * *Poznámka:* Zo všetkých objektov v `WeaponHolder` **odstráň Collider**, aby hráč nekolidoval sám so svojím mečom!

### Krok D: Ostrý Retro Pixel-Art Filter (640x360 Render Texture)
Aby si dostal tie ostré zubaté biele hrany na meči a retro dithering ako na obrázku:
1. V Project okne klikni pravým -> `Create -> Render Texture`, nazvi ju `RT_RetroLowRes`.
2. V jej Inspectore nastav:
   * `Size`: **`640 x 360`** (alebo `480 x 270` pre ešte hrubší pixel).
   * `Anti-Aliasing`: **None**.
   * `Filter Mode`: **Point (no filter)**.
3. Klikni na `Main Camera` (pod `Player`) a do políčka **Target Texture** (alebo **Output Texture**) pretiahni `RT_RetroLowRes`.
4. V Hierarchy vytvor `UI -> Raw Image` (vytvorí sa `Canvas`):
   * Na `Canvas` nastav `UI Scale Mode`: **Scale With Screen Size**.
   * Na `Raw Image` klikni na **Anchor Presets** (vľavo hore v Rect Transform), podrž `Alt + Shift` a klikni na **Stretch-Stretch** (roztiahne sa na celú obrazovku).
   * Do políčka **Texture** na `Raw Image` pretiahni `RT_RetroLowRes`.

### Krok E: Minimalistické 1-bit Pixel UI (Zameriavač a Ikony)
1. **Stredový krížik (Crosshair):** Pod `Canvas` vytvor `UI -> Image` uprostred obrazovky `(0, 0, 0)`, veľkosť `16 x 16`, s malou bielou pixel-art textúrou krížika (`Filter Mode: Point`).
2. **Horný panel a ľavý horný roh:** Pridaj pod `Canvas` biele monochromatické (1-bit) pixel-art ikonky (srdiečka/runy) ukotvené hore vľavo a hore v strede.

---

## 6. Checklist na koniec dnešného dňa (Definition of Done)
- [ ] Máš 1 vygenerovaného Gnolla z Midjourney s bielym okrajom a celými nohami.
- [ ] Prešiel cez 3D generátor a Mixamo bez toho, aby sa mu zdeformovali nohy.
- [ ] V Unity má textúra zapnutý `Point (no filter)` a scéna má zapnutú zelenkavú hmlu (Fog).
- [ ] Scéna obsahuje cintorín (kríže), siluetu hradu v hmle, ruku s mečom pod kamerou a beží cez `640x360` Point-filtered Render Texture.
- [ ] Stlačíš Play, prechádzaš sa pomocou WASD po lese a Gnoll sa za tebou rozbehne v 12-FPS retro animácii a sekne po tebe, keď príde blízko.

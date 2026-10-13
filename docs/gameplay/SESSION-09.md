# Ezra City — larger schools, parks and eastern streets

Latest October9 task completed. User asked for elementary/high-school placement with aesthetics and decoration, a few parks/paths, then the eastern road network. Mid-pass correction: use larger versions of both schools. Preserve the user's revised roads and newly placed fire house/clinic. Earlier road/infill work is in [SESSION-08.md](SESSION-08.md).

## Latest measured state and save

Start treasury2,232,565; final1,611,945; net spending620,620. No simulation elapsed: frame14928842, paused, UI01:48 September2028. No active wall-clock deadline. Population7,418 and service demand are still frozen pre-clearance measurements, not proof of operational recovery.

Pre-work Ezra-Schools-East-Start verified exclusive readable, SaveGameData25,421,521bytes. Final Ezra-Schools-Parks-East verified exclusive readable, SaveGameData25,560,627bytes and texture482,172bytes. Intermediate Ezra-Large-Schools-Parks was requested but not independently verified; use the verified final checkpoint. Raw evidence remains ignored in .local/r4-*.

## Schools and parks

- Full-sized ElementarySchool01, entity72238v395, centered(-1860,673), facing north onto local road atz713; base footprint144×64m. UI showed capacity1,000,0/15 staff,0 students and12,500/month maintenance before staffing. Future staffed cost, actual enrollment and upgrade envelope remain untested.
- Full-sized HighSchool01, entity90444v481, centered(-1972.18,133), facing north onto Coldwater Street atz205; footprint176×128m. Large southwest grounds and college reservation north ofz205 remain open. High-school student capacity was not read from UI; do not invent it from prefab size.
- Three new parks: CityPark02 at(-1835.38,173) beside the high school; CityPark02 at(-1678.07,633) near the elementary neighborhood; CityPark01 at(-1425,1033) in the northern residential loop. Existing northern parks were preserved.
- Tree upgrades applied to four school/park frontage road segments. Added approximately576.6m of pavement paths: elementary shortcut(-1950,601)->(-1766,601), and high-school green-space walk(-2138.18,25)->(-1850,25)->(-1774.18,77). The final all-network graph confirms these attach to the existing street network. Actual pedestrian use has not been observed while paused.

The initially placed smaller ElementarySchool02 and HighSchool02 cost190,000. The user then requested larger schools; the two smaller buildings were removed through normal bulldozing and refunded47,500. Final larger schools cost400,000, parks50,000; remaining28,120 is roads, paths and tree upgrades. The142,500 replacement loss is included in net620,620, not hidden as a school construction cost. No loans or tile purchases.

## Eastern network

21 successful road operations, approximately6,233m. Medium-road collector x≈-854.18 connects existing cross-city roads atz=-188,460.67,1670. Local roads aroundx=-998 form north/south loops that turn back into the collector before the central interchange approach, keeping extra intersections off its immediate ramp approach. The southern waterfront grid usesx≈-742.18. A northern waterfront loop usesx=-710 betweenz1324.67 and1670. Cross-street spacing mostly160–176m; wider blocks/open land retained near the narrow shoreline. One selected link joins the coastal road near(-684.7,994.7).

QuayLarge01 was verified as a24m RoadPrefab with Road-layer connectivity. Preserve it as the user's existing waterfront street. No freeway lanes or ramps were rebuilt, and no eastern zoning was painted in this pass. Waterfront park/service space remains available for later planning.

Final untruncated graph returned754/754 networks; all68 current eastern non-highway road segments in the district are reachable from the central avenue junction. No unintended new east-side dead ends. Four original receipt-node IDs had disappeared during later junction reconstruction; current-graph verification confirmed they were superseded, not disconnected roads. This establishes topology only; capacity and peak traffic remain untested.

## Failures, limits and next step

The first elementary path request was interrupted by another active tool. Fresh graph showed no path, then retry succeeded. A later optional collector-to-quay path was rejected InvalidShape with no change; no forced placement or repeated blind retry. Its second queued path was never attempted. School walking paths are complete; no new direct quay path was built.

Keep the city paused until the user is ready for remaining zoning and essential-service/utility checks. Verify road access, local power/water, police availability, staffing, school enrollment and recovery income after the population rebuilds. FireHouse01 near(-1396,1258) and MedicalClinic01 near(-1858,1043) were already placed by the user before this task. Do not duplicate them based on older records. The day/night visual setting from the earlier construction phase remains unverified; it was not changed in this pass.

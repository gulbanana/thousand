TO DO
=====

P1
--
* triage todo list

P2
--
* class contents placeholder inserts - right now it's all prepended (but how would this work with a classlist?)
* fix errors failing to clear
* use fill-opacity for SVG transparency (https://gitlab.com/inkscape/inbox/-/issues/1195)
* probable syntax changes: alists should be line scopes; clists should be available for lines and *maybe* the only form
* path names (perhaps use [] for anon)
* line labels
* investigate track-span 
* object offset (already parsed/styled)
* direct layout with X/Y - relative to region, can increase bounds (rename to position? perhaps does not support align?)
* using diamond as a container seems to be broken somehow
* (line) anchors and offsets probably don't combine correctly
* anchors should be close to each other instead of strictly clockwise
* consider macro attrkeys (might actually improve errors)
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text... alternatively, just rename min-width to width and caveat it
* reevaluate padding algorithm - is it correct to ignore padding when there is no content? we could disable padding for shape=none instead
* drop shadows
* customisable arrowheads
* triangle text placement is bad visually. special case to move either the text or the shape?
* consider line caps (butt etc) 
* docs - generated for attrs? or just handwrite it all?
* regularity control (a square is a regular rectangle) (not so sure about this, it makes the code simpler but the api more complex...)
* do a defaults pass, picking values which produce a nice appearance without customisation
* image embedding 
* editor integration - plugin with the tokenizer (cannot reuse the one from the website, monarch vs textmate!), actual completion provider/language service, live preview...
* packaging - CI for tool/core pack
* web components lib
* import files
* import templates (not the same as above! applies to the stdlib)
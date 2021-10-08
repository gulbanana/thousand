TO DO
=====

P1
--
* implement composition for pack
* implement composition for justify/align
* either text-anchor and text-offset or anchor and offset for objects, taking them out of the normal layout (or both)
* ArcMidpoint is not correct
* anchors and offsets probably don't combine correctly
* object class contents, with placeholders (need skip-level evaluation here)

P2
--
* class contents templating - probably sub-declaration only 
* improve shorthand expectations
* consider macro attrkeys (might actually improve errors)
* 4-way thickness (for padding and margins)
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text... alternatively, just rename min-width to width and caveat it
* anchors should be close to each other instead of strictly clockwise
* canvas layout with X/Y - and/or support direct X Y in any mode (does this take nodes out of the normal layout?)
* reevaluate padding algorithm - is it correct to ignore padding when there is no content?
* reevaluate x/y specification in offsets and possibly elsewhere (tedium)
* octagon shape
* regularity control (a square is a regular rectangle)
* drop shadows
* customisable arrowheads
* enable fill for shape=none? what does it even mean then?
* DISABLE padding for shape=none? stdlib is getting unwieldy
* we have some throws that could be converted to positioned errors
* docs - generated for attrs? or just handwrite it all?
* consider line caps

P3
--
* do a defaults pass, picking values which produce a nice appearance without customisation
* image embedding 
* editor integration - tokenizer, actual language service, live preview...
* packaging - less trivial CLI (dotnet tool), nuget core, web components (monaco?)
* import files
* import templates (not the same as above! applies to the stdlib)
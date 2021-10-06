TO DO
-----

* 8 specific anchors (8way = all)
* text-anchor, text-offset
* preset shape corners (trapezium needs it)
* 4-way thickness
* implement composition for pack 
* implement composition for justify/align
* canvas layout with X/Y - and/or support direct X Y in any mode
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text...
* alternatively, just rename min-width to width and caveat it
* drop shadows
* reevaluate padding algorithm - is it correct to ignore padding when there is no content?
* anchors and offsets probably don't combine correctly
* reevaluate x/y of offset
* customisable arrowheads
* enable fill for shape=none? what does it even mean then?
* DISABLE padding for shape=none? stdlib is getting unwieldy
* we have some throws that could be converted to positioned errors
* (medium) do a defaults pass, picking values which produce a nice appearance without customisation
* (medium) IdentifierOrKeyword, hyphen formatting
* (large) object templates - might require two-phase parsing :(
* (large) image embedding 
* (large) doc generation, at least for attrs. or just handwrite it all?

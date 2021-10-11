TO DO
=====

P1
--
* object anchor (already parsed/styled)
* object class contents, with placeholders (need skip-level evaluation here)

P2
--
* line labels
* object offset (already parsed/styled)
* canvas layout with X/Y (also parsed/styled) - and/or support direct X Y in any mode (does this take nodes out of the normal layout?)
* X/Y for line start/end? maybe not, we have points and X-start is ugly
* allow min- to influence justification?
* (line) anchors and offsets probably don't combine correctly
* anchors should be close to each other instead of strictly clockwise
* row/col axial justification is weird when packed
* class contents templating - probably sub-declaration only 
* consider macro attrkeys (might actually improve errors)
* bring back width/height somehow - set intrinsic size instead of being overrides? but then you'd have padding, and clipped text... alternatively, just rename min-width to width and caveat it
* reevaluate padding algorithm - is it correct to ignore padding when there is no content? we could disable padding for shape=none instead
* drop shadows
* customisable arrowheads
* docs - generated for attrs? or just handwrite it all?
* consider line caps
* triangle text placement is bad visually. special case to move either the text or the shape?

P3
--
* regularity control (a square is a regular rectangle) (not so sure about this, it makes the code simpler but the api more complex...)
* do a defaults pass, picking values which produce a nice appearance without customisation
* image embedding 
* editor integration - plugin with the tokenizer (cannot reuse the one from the website, monarch vs textmate!), actual completion provider/language service, live preview...
* packaging - less trivial CLI (dotnet tool), nuget core, web components
* import files
* import templates (not the same as above! applies to the stdlib)
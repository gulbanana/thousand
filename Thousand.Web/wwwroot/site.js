function registerLanguage() {
    monaco.languages.register({ id: 'thousand' });

	monaco.languages.setMonarchTokensProvider('thousand', {
		keywords: ['$*', 'class', 'none'],

		escapes: /\\(?:[rn\\"']|u[0-9A-Fa-f]{4})/,

		identifier: /[a-z][\w\-]*/,

		tokenizer: {
			root: [
				// special quasisemantic case: identifier/keywords at the start of a line 
				[/^\s*@identifier\s*=/, 'identifier'],
				[/^\s*@identifier/, {
					cases: {
						'@keywords': 'keyword',
						'@default': 'type'
					}
				}],

				// whitespace and comments
				{ include: '@whitespace' },

				// symbols
				[/[\[\]{}()]/, '@brackets'],
				[/[=,:\.]/, ''],

				// non-identifier keywords
				[/$\*/, 'keyword'],

				// colours
				[/#[0-9A-Fa-f]{3}([0-9A-Fa-f]{3})?/, 'number.hex'],

				// arrows
				[/--*-|<-*-|--*>|<-*>/, 'keyword'],

				// strings
				[/"([^"\\]|\\.)*$/, 'string.invalid'],  // non-teminated string
				[/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],

				// variables
				[/$@identifier/, 'variable'],

				// identifiers and keywords
				[/@identifier/, {
					cases: {
						'@keywords': 'keyword',
						'@default': 'identifier'
					}
				}],

				// numbers
				[/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
				[/\d+/, 'number'],
			],

			comment: [
				[/[^\/*]+/, 'comment'],
				[/\/\*/, 'comment', '@push'],    // nested comment
				["\\*/", 'comment', '@pop'],
				[/[\/*]/, 'comment']
			],

			string: [
				[/[^\\"]+/, 'string'],
				[/@escapes/, 'string.escape'],
				[/\\./, 'string.escape.invalid'],
				[/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }]
			],

			whitespace: [
				[/[ \t\r\n]+/, 'white'],
				[/\/\*/, 'comment', '@comment'],
				[/\/\/.*$/, 'comment'],
			]
		}
	});
}
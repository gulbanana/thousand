function registerLanguage() {
    monaco.languages.register({ id: 'thousand' });

	monaco.languages.setMonarchTokensProvider('thousand', {
		keywords: ['class', 'none'],
		escapes: /\\(?:[rn\\"']|u[0-9A-Fa-f]{4})/,
		tokenizer: {
			root: [
				// types
				[/^[a-z][\w\-]*/, {
					cases: {
						'@keywords': 'keyword',
						'@default': 'type'
                    }
				}],

				// identifiers
				[/[a-z][\w\-]*/, {
					cases: {
						'@keywords': 'keyword',
						'@default': 'identifier'
					}
				}],

				// whitespace and comments
				{ include: '@whitespace' },

				// colours
				[/#[0-9A-Fa-f]{3}([0-9A-Fa-f]{3})?/, 'number'],

				// numbers
				[/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
				[/\d+/, 'number'],

				// strings
				[/"([^"\\]|\\.)*$/, 'string.invalid'],  // non-teminated string
				[/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],

				// arrows
				[/--|<-|->|<>/, 'keyword'],
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
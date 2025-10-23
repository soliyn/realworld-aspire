/** @type {import('jest').Config} */
module.exports = {
    preset: 'jest-preset-angular',
    setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
    testEnvironment: 'jsdom',
    transform: {
        '^.+\\.(ts|js|mjs|html|svg)$': [
            'jest-preset-angular',
            {
                tsconfig: '<rootDir>/tsconfig.spec.json',
                stringifyContentPathRegex: '\\.(html|svg)$',
            },
        ],
    },
    moduleFileExtensions: ['ts', 'html', 'js', 'json', 'mjs'],
    testMatch: ['**/+(*.)+(spec).+(ts)'],
    transformIgnorePatterns: [
        'node_modules/(?!.*\\.mjs$|@angular|@ngrx|rxjs|tslib)',
    ],
};

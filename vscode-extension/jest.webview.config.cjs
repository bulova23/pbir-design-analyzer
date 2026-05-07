/** @type {import('jest').Config} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  roots: ['<rootDir>/webview-src'],
  moduleFileExtensions: ['ts', 'tsx', 'js', 'json'],
  testMatch: ['**/webview-src/**/?(*.)+(spec|test).(ts|tsx)'],
  moduleNameMapper: {
    '\\.(css|less|scss|sass)$': '<rootDir>/tests/__mocks__/styleMock.js',
    '^vscode$': '<rootDir>/tests/__mocks__/vscode.ts',
    '^@/(.*)$': '<rootDir>/webview-src/$1',
  },
  setupFilesAfterEnv: ['<rootDir>/webview-src/setupTests.ts'],
  transform: {
    '^.+\\.tsx?$': ['ts-jest', {
      tsconfig: '<rootDir>/tsconfig.webview-jest.json'
    }]
  }
};

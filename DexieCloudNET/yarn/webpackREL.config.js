const path = require('path')

module.exports = {
    mode: 'production',
    entry: ['./src/dexieCloudNET.ts'],
    module: {
        rules: [
        ]
    },
    experiments: {
        outputModule: true
    },
    output: {
        module: true,
        library: {
            type: 'module'
        },
        path: path.resolve(__dirname, '../wwwroot/js'),
        filename: 'dexieCloudNET.js',
        publicPath: '_content/DexieCloudNET/js/'
    }
}

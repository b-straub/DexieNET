const path = require('path')

module.exports = {
    mode: 'development',
    entry: ['./src/dexieNET.ts'],
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: ["ts-loader"],
                exclude: /node_modules/
            },
        ]
    },
    experiments: {
        outputModule: true
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    devtool: 'source-map',
    output: {
        module: true,
        library: {
            type: 'module'
        },
        path: path.resolve(__dirname, '../wwwroot/js'),
        filename: 'dexieNET.js',
        publicPath: '_content/DexieNET/js/'
    },
    optimization: {
        minimize: false
    }
}

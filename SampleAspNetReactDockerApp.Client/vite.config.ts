import {fileURLToPath, URL} from 'node:url';

import {defineConfig, ProxyOptions} from 'vite';
import plugin from '@vitejs/plugin-react';
import mkcert from "vite-plugin-mkcert";
import https from "https";

// import fs from 'fs';
// import path from 'path';
// import child_process from 'child_process';
// const baseFolder =
//     process.env.APPDATA !== undefined && process.env.APPDATA !== ''
//         ? `${process.env.APPDATA}/ASP.NET/https`
//         : `${process.env.HOME}/.aspnet/https`;
// const certificateArg = process.argv.map(arg => arg.match(/--name=(?<value>.+)/i)).filter(Boolean)[0];
// const certificateName = certificateArg && certificateArg.groups ? certificateArg.groups.value : "sampleaspnetreactdockerapp.client";
// if (!certificateName) {
//     console.error('Invalid certificate name. Run this script in the context of an npm/yarn script or pass --name=<<app>> explicitly.')
//     process.exit(-1);
// }
// const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
// const keyFilePath = path.join(baseFolder, `${certificateName}.key`);
// if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
//     if (0 !== child_process.spawnSync('dotnet', [
//         'dev-certs',
//         'https',
//         '--export-path',
//         certFilePath,
//         '--format',
//         'Pem',
//         '--no-password',
//     ], { stdio: 'inherit', }).status) {
//         throw new Error("Could not create certificate.");
//     }
// }
//process.env.ASPNETCORE_URLS ? process.env.ASPNETCORE_URLS.split(';') : 
const possibleBackendUrls: string[] = ["https://localhost:7191", "https://localhost:7192", "https://localhost:7193"];
//["http://localhost:5136", "http://localhost:5137" , "http://localhost:5138"];

console.log('process.env.ASPNETCORE_URLS is ', process.env.ASPNETCORE_URLS);
const aspNetCore_environment: string = process.env.ASPNETCORE_ENVIRONMENT || "Development";

// The application was designed so that the swagger UI is only shown in development mode or when the ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION environment variable is set to true.
const aspNetCore_shouldShowSwaggerInProduction: boolean = process.env.ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION === "true";
console.log("possibleBackendUrls is ", possibleBackendUrls);
console.log("Currently in ",aspNetCore_environment);
const backendUrl: string = possibleBackendUrls[0];
//possibleBackendUrls.find(url => url.startsWith("https")) || 
console.log(`Main Backend URL: ${backendUrl}`);
const videoBackendUrl: string = possibleBackendUrls[1];
//possibleBackendUrls.find(url => url.startsWith("https")) || 
const pairBackendUrl: string = possibleBackendUrls[2];
//possibleBackendUrls.find(url => url.startsWith("https")) || 
console.log(`VideoShare service Backend URL: ${videoBackendUrl}`);
console.log(`MatchMaker service Backend URL: ${pairBackendUrl}`);

const runAsHttps: boolean = backendUrl.startsWith("https");

const httpsAgent = new https.Agent({
    rejectUnauthorized: false // This will ignore certificate errors
});


let serverProxies: Record<string, ProxyOptions> =
    runAsHttps ?
        {
            "/api": {
                target: backendUrl,
                agent: httpsAgent
            },
            '/videoShareHub': {
                target: videoBackendUrl,
                ws: true,
                changeOrigin: true,
                secure: false,
            },
            "/vid/api": {
                target: videoBackendUrl,
                agent: httpsAgent,
                changeOrigin: true,
                secure: true,
                rewrite: (path) => {
                    const newPath = path.replace(/^\/vid\/api/, '/api');
                    console.log(`Proxying ${path} to ${videoBackendUrl}${newPath}`);
                    return newPath;
                },
            },
            "/pair/api": {
                target: pairBackendUrl,
                agent: httpsAgent,
                changeOrigin: true,
                secure: true,
            },
        }
        : 
        {
            "/api": {
                target: backendUrl,
            },
            "/videoShareHub": {
                target: videoBackendUrl,
                ws: true,
                changeOrigin: true,
                secure: false,
            },
            
            "/vid/api": {
                target: videoBackendUrl,
                changeOrigin: true,
                secure: false,
                rewrite: (path) => {
                    const newPath = path.replace(/^\/vid\/api/, '/api');
                    console.log(`Proxying ${path} to ${videoBackendUrl}${newPath}`);
                    return newPath;
                },
            },
            "/pair/api": {
                target: pairBackendUrl,
                changeOrigin: true,
                secure: false,
            },
        }

if (aspNetCore_environment === "Development" || aspNetCore_shouldShowSwaggerInProduction) {
    serverProxies = runAsHttps ? {
        ...serverProxies,
        "/swagger": {
            target: backendUrl,
            agent: httpsAgent
        },
    } 
    : {
            ...serverProxies,
            "/swagger": {
                target: backendUrl,
            },
        
        }
}


// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), mkcert()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    build: {
        outDir: 'dist',
        assetsDir: 'assets',
        sourcemap: false
    },
    server: {
        proxy: serverProxies,
        port: 5173,
        // https: {
        //     key: fs.readFileSync(keyFilePath),
        //     cert: fs.readFileSync(certFilePath),
        // }
    }
})

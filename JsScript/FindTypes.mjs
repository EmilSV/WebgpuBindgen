import { readFileSync, writeFileSync } from "fs";
const pathToFile = process.argv[2];
const pathToOutput = process.argv[3];
const fileContent = readFileSync(pathToFile, "utf8");

const jsonContent = JSON.parse(fileContent);

const types = new Set();


/**
 * 
 * @param {Record<string,any>} jsonContent 
 * @param {string[]} prevPath 
 * @returns 
 */
function findTypes(jsonContent, prevPath) 
{
    if (typeof jsonContent !== "object")
    {
        return;
    }

    for (const key in jsonContent)
    {
        const newPath = prevPath.slice();
        newPath.push(key);
        const value = jsonContent[key];
        if (typeof value === "object")
        {
            if (Array.isArray(value))
            {
                for (const item of value)
                {
                    const newPathArray = newPath.slice();
                    newPathArray.push("$ArrayItem");
                    findTypes(item, newPathArray);
                }
            }
            else
            {
                findTypes(value, newPath);
            }
        }
        else if (typeof value === "string" && key === "type")
        {
            newPath.shift();
            types.add(`${newPath.join(".")}=${value}`);
        }
    }
}

findTypes(jsonContent, []);

const outContent = Array.from(types).sort().join("\n");

writeFileSync(pathToOutput, outContent, "utf8");

from openai import OpenAI
import json

client = OpenAI(api_key='APIKEY')
# Set your OpenAI API key

# Read your JSON file
with open('asdasd.json', 'r', encoding='utf-8') as file:
    data = json.load(file)

# Function to translate text using OpenAI
def translate_text(text, target_language='vi'):
    response = client.completions.create(model="gpt-3.5-turbo-instruct",  # or the latest model you want to use
    prompt=f"Translate the following English text to {target_language}: {text}",
    max_tokens=60)
    return response.choices[0].text.strip()

# Translate each text
translated_data = {}

for key, text_value in data.items():
    translated_text = translate_text(text_value)
    translated_data[key] = translated_text

# Save the translated data to a new JSON file with UTF-8 encoding
output_file = 'translated_texts.json'
with open(output_file, 'w', encoding='utf-8') as outfile:
    json.dump(translated_data, outfile, ensure_ascii=False, indent=2)

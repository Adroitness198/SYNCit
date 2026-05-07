from groq import Groq
import sys
import json

def analyse(cv_text, job_description):
    client = Groq(api_key="GROQ_API_KEY")

    prompt = f"""
    You are a professional CV and job description analyst.
    
    Analyse the following CV against the job description and return ONLY a JSON object with no extra text, no markdown, no backticks.
    
    The JSON must have exactly these fields:
    {{
        "match_score": <number 0-100>,
        "strengths": [<list of 3-5 strings>],
        "gaps": [<list of 3-5 strings>],
        "recommendation": "<one paragraph of advice>"
    }}
    
    CV:
    {cv_text}
    
    Job Description:
    {job_description}
    """

    response = client.chat.completions.create(
        model="llama-3.3-70b-versatile",
        messages=[{"role": "user", "content": prompt}]
    )
    return response.choices[0].message.content

if __name__ == "__main__":
    with open(sys.argv[1], 'r', encoding='utf-8') as f:
        cv = f.read()
    with open(sys.argv[2], 'r', encoding='utf-8') as f:
        jd = f.read()
    result = analyse(cv, jd)
    print(result)